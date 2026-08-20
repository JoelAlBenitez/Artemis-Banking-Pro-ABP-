using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.ChangeUserStatus;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.CreateUser;
using Artemis_Banking_Pro.Core.Application.Features.Users.Commands.UpdateUser;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Registration;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Account;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Users
{
    public sealed class UsersCommandHandlersTests
    {
        private readonly Mock<IAccountRegistrationService> _registrationService = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<ICommerceRepository> _commerceRepository = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        #region Crear usuario

        //Los usuarios creados desde la API quedan inactivos hasta confirmar la cuenta, y el
        //correo lleva el token en el cuerpo: por eso Origin viaja en null.
        [Fact]
        public async Task CreateUser_ShouldCreateItInactiveAndWithoutActivationLink()
        {
            _registrationService
                .Setup(service => service.RegisterUserAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(new RegisterResponse { UserId = "2" });

            var handler = new CreateUserCommandHandler(_registrationService.Object);

            var result = await handler.Handle(BuildCreateUserCommand(), CancellationToken.None);

            result.Id.Should().Be("2");
            result.IsActive.Should().BeFalse();
            result.Role.Should().Be(nameof(Roles.Cliente));

            _registrationService.Verify(service => service.RegisterUserAsync(
                It.Is<RegisterRequest>(request => request.Origin == null)), Times.Once);
        }

        [Fact]
        public async Task CreateUser_WithDuplicatedData_ShouldReportItAsConflict()
        {
            _registrationService
                .Setup(service => service.RegisterUserAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(new RegisterResponse
                {
                    HasError = true,
                    Conflict = true,
                    Error = "El nombre de usuario ya está registrado."
                });

            var handler = new CreateUserCommandHandler(_registrationService.Object);

            var act = async () => await handler.Handle(BuildCreateUserCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("El nombre de usuario ya está registrado.");
        }

        #endregion

        #region Crear usuario de comercio

        //El rol no se recibe en el body: siempre es Comercio.
        [Fact]
        public async Task CreateCommerceUser_ShouldAssignTheCommerceRoleAndLinkTheCommerce()
        {
            var commerce = BuildCommerce(associatedUserId: null);

            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(commerce);
            _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");

            _registrationService
                .Setup(service => service.RegisterUserAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(new RegisterResponse { UserId = "10" });

            var handler = new CreateCommerceUserCommandHandler(
                _registrationService.Object, _commerceRepository.Object, _currentUserService.Object);

            var result = await handler.Handle(BuildCreateCommerceUserCommand(), CancellationToken.None);

            result.Role.Should().Be(nameof(Roles.Comercio));
            result.CommerceId.Should().Be(5);
            result.IsActive.Should().BeFalse();

            _registrationService.Verify(service => service.RegisterUserAsync(
                It.Is<RegisterRequest>(request => request.Role == nameof(Roles.Comercio))), Times.Once);

            commerce.AssociatedUserId.Should().Be("10");
            _commerceRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateCommerceUser_WithUnknownCommerce_ShouldReportItAsNotFound()
        {
            _commerceRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Commerce)null!);

            var handler = new CreateCommerceUserCommandHandler(
                _registrationService.Object, _commerceRepository.Object, _currentUserService.Object);

            var act = async () => await handler.Handle(BuildCreateCommerceUserCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            //El usuario no debe llegar a crearse si el comercio no existe
            _registrationService.Verify(
                service => service.RegisterUserAsync(It.IsAny<RegisterRequest>()), Times.Never);
        }

        //Un comercio admite un solo usuario asociado.
        [Fact]
        public async Task CreateCommerceUser_WhenTheCommerceAlreadyHasUser_ShouldReportItAsConflict()
        {
            _commerceRepository.Setup(repository => repository.GetByIdAsync(5))
                .ReturnsAsync(BuildCommerce(associatedUserId: "10"));

            var handler = new CreateCommerceUserCommandHandler(
                _registrationService.Object, _commerceRepository.Object, _currentUserService.Object);

            var act = async () => await handler.Handle(BuildCreateCommerceUserCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("Este comercio ya tiene un usuario asociado.");

            _registrationService.Verify(
                service => service.RegisterUserAsync(It.IsAny<RegisterRequest>()), Times.Never);
        }

        #endregion

        #region Actualizar usuario

        //Sin contraseña en el body la actual no se toca: el handler reenvía el null tal cual.
        [Fact]
        public async Task UpdateUser_WithoutPassword_ShouldNotSendANewOne()
        {
            _userManagementService
                .Setup(service => service.UpdateUserAsync(It.IsAny<string>(), It.IsAny<EditUserDto>()))
                .ReturnsAsync(new UserOperationResponseDto());

            var handler = new UpdateUserCommandHandler(_userManagementService.Object);

            await handler.Handle(BuildUpdateUserCommand(), CancellationToken.None);

            _userManagementService.Verify(service => service.UpdateUserAsync(
                "2", It.Is<EditUserDto>(dto => dto.NewPassword == null)), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldForwardTheAdditionalAmount()
        {
            _userManagementService
                .Setup(service => service.UpdateUserAsync(It.IsAny<string>(), It.IsAny<EditUserDto>()))
                .ReturnsAsync(new UserOperationResponseDto());

            var handler = new UpdateUserCommandHandler(_userManagementService.Object);

            var command = BuildUpdateUserCommand();
            command.AdditionalAmount = 12_000m;

            await handler.Handle(command, CancellationToken.None);

            _userManagementService.Verify(service => service.UpdateUserAsync(
                "2", It.Is<EditUserDto>(dto => dto.AdditionalAmount == 12_000m)), Times.Once);
        }

        [Theory]
        [InlineData(true, false, typeof(NotFoundException))]
        [InlineData(false, true, typeof(ConflictException))]
        [InlineData(false, false, typeof(BusinessRuleException))]
        public async Task UpdateUser_WithError_ShouldTranslateItToItsOwnResponse(
            bool notFound, bool conflict, Type expectedException)
        {
            _userManagementService
                .Setup(service => service.UpdateUserAsync(It.IsAny<string>(), It.IsAny<EditUserDto>()))
                .ReturnsAsync(new UserOperationResponseDto
                {
                    HasError = true,
                    NotFound = notFound,
                    Conflict = conflict,
                    Error = "Detalle del rechazo."
                });

            var handler = new UpdateUserCommandHandler(_userManagementService.Object);

            var act = async () => await handler.Handle(BuildUpdateUserCommand(), CancellationToken.None);

            (await act.Should().ThrowAsync<Exception>()).Which.Should().BeOfType(expectedException);
        }

        #endregion

        #region Cambiar estado

        [Fact]
        public async Task ChangeUserStatus_ShouldForwardTheRequestedState()
        {
            _userManagementService
                .Setup(service => service.SetUserStatusAsync("2", true))
                .ReturnsAsync(new UserOperationResponseDto());

            var handler = new ChangeUserStatusCommandHandler(_userManagementService.Object);

            await handler.Handle(
                new ChangeUserStatusCommand { Id = "2", Status = true }, CancellationToken.None);

            _userManagementService.Verify(service => service.SetUserStatusAsync("2", true), Times.Once);
        }

        //El intento de auto-modificación se responde como acceso denegado, no como dato inválido.
        [Fact]
        public async Task ChangeUserStatus_OnTheOwnAccount_ShouldRejectItAsForbidden()
        {
            _userManagementService
                .Setup(service => service.SetUserStatusAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new UserOperationResponseDto
                {
                    HasError = true,
                    Error = "No puede modificar el estado de su propia cuenta."
                });

            var handler = new ChangeUserStatusCommandHandler(_userManagementService.Object);

            var act = async () => await handler.Handle(
                new ChangeUserStatusCommand { Id = "admin-1", Status = false }, CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("No puede modificar el estado de su propia cuenta.");
        }

        [Fact]
        public async Task ChangeUserStatus_WithUnknownUser_ShouldReportItAsNotFound()
        {
            _userManagementService
                .Setup(service => service.SetUserStatusAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new UserOperationResponseDto
                {
                    HasError = true,
                    NotFound = true,
                    Error = "El usuario seleccionado no existe."
                });

            var handler = new ChangeUserStatusCommandHandler(_userManagementService.Object);

            var act = async () => await handler.Handle(
                new ChangeUserStatusCommand { Id = "inexistente", Status = true }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region builders

        private static CreateUserCommand BuildCreateUserCommand()
            => new()
            {
                FirstName = "Maria",
                LastName = "Gomez",
                Identification = "00187654321",
                Email = "cliente01@artemis.com",
                UserName = "cliente01",
                Password = "123P@$$word!",
                ConfirmPassword = "123P@$$word!",
                Role = nameof(Roles.Cliente),
                InitialAmount = 5_000m
            };

        private static CreateCommerceUserCommand BuildCreateCommerceUserCommand()
            => new()
            {
                CommerceId = 5,
                FirstName = "Usuario",
                LastName = "Comercio",
                Identification = "10199999999",
                Email = "commerce01@artemis.com",
                UserName = "commerce01",
                Password = "123P@$$word!",
                ConfirmPassword = "123P@$$word!",
                InitialAmount = 0m
            };

        private static UpdateUserCommand BuildUpdateUserCommand()
            => new()
            {
                Id = "2",
                FirstName = "Maria",
                LastName = "Gomez",
                Identification = "00187654321",
                Email = "maria.gomez@artemis.com",
                UserName = "cliente01"
            };

        private static Commerce BuildCommerce(string? associatedUserId)
            => new()
            {
                Id = 5,
                Name = "Tienda Demo",
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999",
                Status = CommerceStatus.Activo,
                AssociatedUserId = associatedUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };

        #endregion
    }
}
