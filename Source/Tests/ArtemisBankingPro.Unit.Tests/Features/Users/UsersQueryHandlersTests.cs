using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Exceptions;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetAllUsers;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetCommerceUsers;
using Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetUserById;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Common;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Features.Users
{
    public sealed class UsersQueryHandlersTests
    {
        private const string ClientId = "cliente-1";

        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<ICommerceRepository> _commerceRepository = new();
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();

        #region Listado general

        //El documento exige la forma page/pageSize/totalRecords/totalPages/data, no la del
        //contrato interno de Identity.
        [Fact]
        public async Task GetAllUsers_ShouldProjectTheDocumentedShape()
        {
            _userManagementService
                .Setup(service => service.GetUsersAsync(1, 20, StatusFilter.Todos))
                .ReturnsAsync(BuildPage(BuildUser("1", "admin", Roles.Administrador, active: true)));

            var handler = new GetAllUsersQueryHandler(_userManagementService.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

            result.Page.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.TotalRecords.Should().Be(1);
            result.TotalPages.Should().Be(1);

            var user = result.Data.Single();
            user.Id.Should().Be("1");
            user.UserName.Should().Be("admin");
            user.Role.Should().Be(nameof(Roles.Administrador));
            user.IsActive.Should().BeTrue();
        }

        //Sin filtro se pide el listado completo; con filtro, el contrato específico por rol.
        [Fact]
        public async Task GetAllUsers_WithRoleFilter_ShouldQueryByRole()
        {
            _userManagementService
                .Setup(service => service.GetUsersByRoleAsync(Roles.Cliente, 1, 20))
                .ReturnsAsync(BuildPage(BuildUser("2", "cliente01", Roles.Cliente, active: false)));

            var handler = new GetAllUsersQueryHandler(_userManagementService.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(
                new GetAllUsersQuery { Role = "cliente" }, CancellationToken.None);

            result.Data.Single().Role.Should().Be(nameof(Roles.Cliente));

            _userManagementService.Verify(service => service.GetUsersByRoleAsync(Roles.Cliente, 1, 20), Times.Once);
            _userManagementService.Verify(
                service => service.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<StatusFilter>()),
                Times.Never);
        }

        #endregion

        #region Listado de usuarios de comercio

        //El comercio vive en Persistence y el usuario en Identity: el handler los compone para
        //poder devolver commerceId y commerceName.
        [Fact]
        public async Task GetCommerceUsers_ShouldAttachTheAssociatedCommerce()
        {
            _userManagementService
                .Setup(service => service.GetCommerceUsersAsync(1, 20))
                .ReturnsAsync(BuildPage(BuildUser("10", "commerce01", Roles.Comercio, active: true)));

            _commerceRepository
                .Setup(repository => repository.GetAllFindAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync([BuildCommerce(5, "Tienda Demo", associatedUserId: "10")]);

            var handler = new GetCommerceUsersQueryHandler(
                _userManagementService.Object, _commerceRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCommerceUsersQuery(), CancellationToken.None);

            var user = result.Data.Single();
            user.CommerceId.Should().Be(5);
            user.CommerceName.Should().Be("Tienda Demo");
        }

        //Un usuario Comercio cuyo comercio fue borrado del listado no puede romper la consulta.
        [Fact]
        public async Task GetCommerceUsers_WithoutMatchingCommerce_ShouldLeaveTheCommerceEmpty()
        {
            _userManagementService
                .Setup(service => service.GetCommerceUsersAsync(1, 20))
                .ReturnsAsync(BuildPage(BuildUser("10", "commerce01", Roles.Comercio, active: true)));

            _commerceRepository
                .Setup(repository => repository.GetAllFindAsync(
                    It.IsAny<Expression<Func<Commerce, bool>>>(),
                    It.IsAny<Expression<Func<Commerce, object>>[]>()))
                .ReturnsAsync([]);

            var handler = new GetCommerceUsersQueryHandler(
                _userManagementService.Object, _commerceRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetCommerceUsersQuery(), CancellationToken.None);

            result.Data.Single().CommerceId.Should().BeNull();
        }

        #endregion

        #region Detalle

        [Fact]
        public async Task GetUserById_ShouldIncludeTheMainAccount()
        {
            _userManagementService
                .Setup(service => service.GetUserByIdAsync(ClientId))
                .ReturnsAsync(BuildDetail());

            _savingsAccountsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(BuildMainAccount());

            var handler = new GetUserByIdQueryHandler(
                _userManagementService.Object, _savingsAccountsRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetUserByIdQuery { Id = ClientId }, CancellationToken.None);

            result.MainAccount.Should().NotBeNull();
            result.MainAccount!.AccountNumber.Should().Be("500000001");
            result.MainAccount.IsPrincipal.Should().BeTrue();
            result.MainAccount.Status.Should().Be(nameof(SavingsAccountStatus.Activa));
        }

        //Administrador y Cajero no tienen cuenta principal: la respuesta debe salir igual.
        [Fact]
        public async Task GetUserById_WithoutMainAccount_ShouldReturnTheUserAnyway()
        {
            _userManagementService
                .Setup(service => service.GetUserByIdAsync(ClientId))
                .ReturnsAsync(BuildDetail());

            _savingsAccountsRepository
                .Setup(repository => repository.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((SavingsAccount?)null);

            var handler = new GetUserByIdQueryHandler(
                _userManagementService.Object, _savingsAccountsRepository.Object, ApiMapperFactory.Create());

            var result = await handler.Handle(new GetUserByIdQuery { Id = ClientId }, CancellationToken.None);

            result.MainAccount.Should().BeNull();
            result.UserName.Should().Be("cliente01");
        }

        [Fact]
        public async Task GetUserById_WithUnknownUser_ShouldReportItAsNotFound()
        {
            _userManagementService
                .Setup(service => service.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((UserDetailDto?)null);

            var handler = new GetUserByIdQueryHandler(
                _userManagementService.Object, _savingsAccountsRepository.Object, ApiMapperFactory.Create());

            var act = async () => await handler.Handle(
                new GetUserByIdQuery { Id = "inexistente" }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region builders

        private static PagedResponseDto<UserDto> BuildPage(params UserDto[] users)
            => new() { Items = [.. users], TotalCount = users.Length, Page = 1, PageSize = 20 };

        private static UserDto BuildUser(string id, string userName, Roles role, bool active)
            => new()
            {
                IdUser = id,
                Name = "Nombre",
                LastName = "Apellido",
                UserName = userName,
                Email = $"{userName}@artemis.com",
                IDCARD = "00187654321",
                State = active,
                TypeUser = role
            };

        private static UserDetailDto BuildDetail()
            => new()
            {
                Id = ClientId,
                UserName = "cliente01",
                Name = "Maria",
                LastName = "Gomez",
                IDCARD = "00187654321",
                Email = "cliente01@artemis.com",
                TypeUser = Roles.Cliente,
                State = true,
                IsClient = true
            };

        private static SavingsAccount BuildMainAccount()
            => new()
            {
                Id = 1,
                AccountNumber = "500000001",
                CustomerId = ClientId,
                Balance = 17_000m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

        private static Commerce BuildCommerce(int id, string name, string associatedUserId)
            => new()
            {
                Id = id,
                Name = name,
                Email = "contacto@tiendademo.com",
                PhoneNumber = "8095551234",
                Rnc = "101999999",
                Status = CommerceStatus.Activo,
                AssociatedUserId = associatedUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

        #endregion
    }
}
