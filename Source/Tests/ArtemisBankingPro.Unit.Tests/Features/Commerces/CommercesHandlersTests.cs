using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.ChangeCommerceStatus;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Commands.UpdateCommerce;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetAllCommerces;
using Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetCommerceById;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Commerces
{
    public sealed class CommercesHandlersTests
    {
        private readonly Mock<ICommerceRepository> _commerceRepository = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        #region Listado

        //Sin filtro explícito el documento exige mostrar solo los activos.
        [Fact]
        public async Task GetAllCommerces_WithoutStatusFilter_ShouldQueryOnlyTheActiveOnes()
        {
            _commerceRepository
                .Setup(repository => repository.GetPagedCommercesAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CommerceStatus?>()))
                .ReturnsAsync(new PagedResult<Commerce>([BuildCommerce()], 1, 20, 1));

            var handler = new GetAllCommercesQueryHandler(_commerceRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllCommercesQuery(), CancellationToken.None);

            result.Data.Single().Name.Should().Be("Tienda Demo");

            _commerceRepository.Verify(repository => repository.GetPagedCommercesAsync(
                1, 20, CommerceStatus.Activo), Times.Once);
        }

        //«todos» es el único valor que quita el filtro de estado.
        [Fact]
        public async Task GetAllCommerces_WithAllFilter_ShouldNotFilterByStatus()
        {
            _commerceRepository
                .Setup(repository => repository.GetPagedCommercesAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CommerceStatus?>()))
                .ReturnsAsync(new PagedResult<Commerce>([], 1, 20, 0));

            var handler = new GetAllCommercesQueryHandler(_commerceRepository.Object, ApiMapperFactory.Create());

            await handler.Handle(new GetAllCommercesQuery { Status = "todos" }, CancellationToken.None);

            _commerceRepository.Verify(repository => repository.GetPagedCommercesAsync(1, 20, null), Times.Once);
        }

        [Fact]
        public async Task GetAllCommerces_ShouldReportWhetherTheCommerceHasUser()
        {
            _commerceRepository
                .Setup(repository => repository.GetPagedCommercesAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CommerceStatus?>()))
                .ReturnsAsync(new PagedResult<Commerce>([BuildCommerce(associatedUserId: "10")], 1, 20, 1));

            var handler = new GetAllCommercesQueryHandler(_commerceRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllCommercesQuery(), CancellationToken.None);

            result.Data.Single().HasAssociatedUser.Should().BeTrue();
        }

        #endregion

        #region Detalle

        [Fact]
        public async Task GetCommerceById_ShouldComposeTheAssociatedUserFromIdentity()
        {
            _commerceRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync(BuildCommerce(associatedUserId: "10"));

            _userManagementService
                .Setup(service => service.GetUserByIdAsync("10"))
                .ReturnsAsync(new UserDetailDto
                {
                    Id = "10",
                    UserName = "commerce01",
                    Name = "Usuario",
                    LastName = "Comercio",
                    IDCARD = "10199999999",
                    Email = "commerce01@artemis.com",
                    TypeUser = Roles.Comercio,
                    State = true
                });

            var handler = new GetCommerceByIdQueryHandler(
                _commerceRepository.Object, _userManagementService.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCommerceByIdQuery { Id = 5 }, CancellationToken.None);

            result.AssociatedUser.Should().NotBeNull();
            result.AssociatedUser!.UserName.Should().Be("commerce01");
            result.AssociatedUser.IsActive.Should().BeTrue();
        }

        //Un comercio recién creado todavía no tiene usuario: no debe consultarse a Identity.
        [Fact]
        public async Task GetCommerceById_WithoutAssociatedUser_ShouldNotQueryIdentity()
        {
            _commerceRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync(BuildCommerce());

            var handler = new GetCommerceByIdQueryHandler(
                _commerceRepository.Object, _userManagementService.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCommerceByIdQuery { Id = 5 }, CancellationToken.None);

            result.AssociatedUser.Should().BeNull();
            _userManagementService.Verify(
                service => service.GetUserByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCommerceById_WithUnknownCommerce_ShouldReportItAsNotFound()
        {
            _commerceRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync((Commerce?)null);

            var handler = new GetCommerceByIdQueryHandler(
                _commerceRepository.Object, _userManagementService.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(
                new GetCommerceByIdQuery { Id = 999 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("El comercio seleccionado no existe.");
        }

        #endregion

        #region Creación

        [Fact]
        public async Task CreateCommerce_ShouldCreateItActiveAndRecordTheAdministrator()
        {
            _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
            _commerceRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<Commerce, bool>>>()))
                .ReturnsAsync(false);

            Commerce? saved = null;
            _commerceRepository
                .Setup(repository => repository.AddAsync(It.IsAny<Commerce>()))
                .Callback<Commerce>(commerce => saved = commerce)
                .ReturnsAsync((Commerce commerce) => commerce);

            var handler = new CreateCommerceCommandHandler(
                _commerceRepository.Object, _currentUserService.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            result.IsActive.Should().BeTrue();
            saved!.Status.Should().Be(CommerceStatus.Activo);
            saved.CreateByUserId.Should().Be("admin-1");
        }

        [Theory]
        [InlineData("Ya existe un comercio registrado con este RNC.")]
        [InlineData("Ya existe un comercio registrado con este correo electrónico.")]
        public async Task CreateCommerce_WithDuplicatedRncOrEmail_ShouldReportItAsConflict(string expected)
        {
            _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");

            //La primera comprobación es el RNC y la segunda el correo: el orden decide el mensaje
            var isRncCase = expected.Contains("RNC");
            _commerceRepository
                .SetupSequence(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<Commerce, bool>>>()))
                .ReturnsAsync(isRncCase)
                .ReturnsAsync(true);

            var handler = new CreateCommerceCommandHandler(
                _commerceRepository.Object, _currentUserService.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>().WithMessage(expected);
            _commerceRepository.Verify(repository => repository.AddAsync(It.IsAny<Commerce>()), Times.Never);
        }

        #endregion

        #region Actualización

        //El estado no se modifica desde este endpoint: tiene el suyo propio.
        [Fact]
        public async Task UpdateCommerce_ShouldNotChangeTheStatus()
        {
            var commerce = BuildCommerce();
            commerce.Status = CommerceStatus.Inactivo;

            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(commerce);
            _commerceRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<Commerce, bool>>>()))
                .ReturnsAsync(false);

            var handler = new UpdateCommerceCommandHandler(
                _commerceRepository.Object, _currentUserService.Object);

            await handler.Handle(new UpdateCommerceCommand
            {
                Id = 5,
                Name = "Tienda Demo Actualizada",
                Email = "nuevo@tiendademo.com",
                PhoneNumber = "8095555678",
                Rnc = "101999999"
            }, CancellationToken.None);

            commerce.Name.Should().Be("Tienda Demo Actualizada");
            commerce.Status.Should().Be(CommerceStatus.Inactivo);
        }

        [Fact]
        public async Task UpdateCommerce_WithRncFromAnotherCommerce_ShouldReportItAsConflict()
        {
            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(BuildCommerce());
            _commerceRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<Commerce, bool>>>()))
                .ReturnsAsync(true);

            var handler = new UpdateCommerceCommandHandler(
                _commerceRepository.Object, _currentUserService.Object);

            var act = async () => await handler.Handle(new UpdateCommerceCommand
            {
                Id = 5,
                Name = "Tienda Demo",
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region Cambio de estado

        //Desactivar un comercio arrastra a sus usuarios asociados.
        [Fact]
        public async Task ChangeCommerceStatus_WhenDeactivating_ShouldInactivateTheAssociatedUser()
        {
            var commerce = BuildCommerce(associatedUserId: "10");
            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(commerce);
            _userManagementService
                .Setup(service => service.SetUserStatusAsync("10", false))
                .ReturnsAsync(new UserOperationResponseDto());

            var handler = BuildStatusHandler();

            await handler.Handle(
                new ChangeCommerceStatusCommand { Id = 5, Status = false }, CancellationToken.None);

            commerce.Status.Should().Be(CommerceStatus.Inactivo);
            _userManagementService.Verify(service => service.SetUserStatusAsync("10", false), Times.Once);
        }

        //Reactivar el comercio no reactiva a sus usuarios: deben restablecer su contraseña.
        [Fact]
        public async Task ChangeCommerceStatus_WhenReactivating_ShouldNotActivateTheAssociatedUser()
        {
            var commerce = BuildCommerce(associatedUserId: "10");
            commerce.Status = CommerceStatus.Inactivo;
            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(commerce);

            var handler = BuildStatusHandler();

            await handler.Handle(
                new ChangeCommerceStatusCommand { Id = 5, Status = true }, CancellationToken.None);

            commerce.Status.Should().Be(CommerceStatus.Activo);
            _userManagementService.Verify(
                service => service.SetUserStatusAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        //Identity y Persistence no comparten transacción: si la cascada falla, el cambio de
        //estado del comercio se mantiene y el fallo queda registrado.
        [Fact]
        public async Task ChangeCommerceStatus_WhenTheCascadeFails_ShouldKeepTheCommerceInactive()
        {
            var commerce = BuildCommerce(associatedUserId: "10");
            _commerceRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(commerce);
            _userManagementService
                .Setup(service => service.SetUserStatusAsync("10", false))
                .ReturnsAsync(new UserOperationResponseDto { HasError = true, Error = "Identity no disponible." });

            var handler = BuildStatusHandler();

            var act = async () => await handler.Handle(
                new ChangeCommerceStatusCommand { Id = 5, Status = false }, CancellationToken.None);

            await act.Should().NotThrowAsync();
            commerce.Status.Should().Be(CommerceStatus.Inactivo);
        }

        [Fact]
        public async Task ChangeCommerceStatus_WithUnknownCommerce_ShouldReportItAsNotFound()
        {
            _commerceRepository.Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Commerce)null!);

            var handler = BuildStatusHandler();

            var act = async () => await handler.Handle(
                new ChangeCommerceStatusCommand { Id = 999, Status = false }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region builders

        private ChangeCommerceStatusCommandHandler BuildStatusHandler()
            => new(_commerceRepository.Object,
                   _userManagementService.Object,
                   _currentUserService.Object,
                   NullLogger<ChangeCommerceStatusCommandHandler>.Instance);

        private static CreateCommerceCommand BuildCreateCommand()
            => new()
            {
                Name = "Tienda Demo",
                Description = "Comercio de prueba para pagos Hermes Pay",
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999"
            };

        private static Commerce BuildCommerce(string? associatedUserId = null)
            => new()
            {
                Id = 5,
                Name = "Tienda Demo",
                Description = "Comercio de prueba para pagos Hermes Pay",
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
