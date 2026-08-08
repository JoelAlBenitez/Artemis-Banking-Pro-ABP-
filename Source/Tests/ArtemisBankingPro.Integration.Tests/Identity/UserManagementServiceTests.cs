using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Identity.Services.Management;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Identity
{
    //Mantenimiento de usuarios contra el UserManager real. Cubre las reglas del contrato
    //del módulo de Administración: exclusión del rol Comercio, paginación máxima de 20,
    //orden descendente por fecha y los mensajes exactos del documento funcional.
    public sealed class UserManagementServiceTests : IDisposable
    {
        private readonly IdentityTestHost _host;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository;
        private readonly Mock<ITransactionRepository> _transactionRepository;
        private readonly UserManagementService _service;

        public UserManagementServiceTests()
        {
            _host = new IdentityTestHost();
            _savingsAccountsRepository = new Mock<ISavingsAccountsRepository>();
            _transactionRepository = new Mock<ITransactionRepository>();

            _service = new UserManagementService(
                _host.UserManager,
                _host.RoleManager,
                _host.Mapper,
                NullLogger<UserManagementService>.Instance,
                _savingsAccountsRepository.Object,
                _transactionRepository.Object);
        }

        // ─── Listados ────────────────────────────────────────────────────────

        //Regla transversal: el rol Comercio nunca se lista
        [Fact]
        public async Task GetUsersAsync_ShouldNeverIncludeUsersWithTheCommerceRole()
        {
            await _host.GivenUserAsync(Roles.Administrador, "admin01");
            await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            await _host.GivenUserAsync(Roles.Comercio, "comercio01");

            var result = await _service.GetUsersAsync(1, 20, StatusFilter.Todos);

            result.TotalCount.Should().Be(2);
            result.Items.Should().NotContain(user => user.TypeUser == Roles.Comercio);
        }

        //El listado va del usuario más reciente al más antiguo, sin anteponer los activos
        [Fact]
        public async Task GetUsersAsync_ShouldOrderFromTheMostRecentToTheOldest()
        {
            var today = DateTimeOffset.UtcNow;
            await _host.GivenUserAsync(Roles.Cliente, "antiguo", createdAt: today.AddDays(-30));
            await _host.GivenUserAsync(Roles.Cliente, "reciente", isActive: false, createdAt: today);
            await _host.GivenUserAsync(Roles.Cajero, "intermedio", createdAt: today.AddDays(-5));

            var result = await _service.GetUsersAsync(1, 20, StatusFilter.Todos);

            result.Items.Select(user => user.UserName)
                .Should().Equal("reciente", "intermedio", "antiguo");
        }

        [Fact]
        public async Task GetUsersAsync_WithTheInactiveFilter_ShouldReturnOnlyInactiveUsers()
        {
            await _host.GivenUserAsync(Roles.Cliente, "activo01");
            await _host.GivenUserAsync(Roles.Cliente, "inactivo01", isActive: false);

            var result = await _service.GetUsersAsync(1, 20, StatusFilter.Inactivos);

            result.Items.Should().ContainSingle().Which.UserName.Should().Be("inactivo01");
        }

        //Ningún listado administrativo puede devolver más de 20 registros por página
        [Fact]
        public async Task GetUsersAsync_WithAPageSizeAboveTheLimit_ShouldCapItAtTwenty()
        {
            for (var i = 0; i < 25; i++)
                await _host.GivenUserAsync(Roles.Cliente, $"cliente{i:00}");

            var result = await _service.GetUsersAsync(1, 100, StatusFilter.Todos);

            result.Items.Should().HaveCount(DomainConstants.MaxPageSize);
            result.PageSize.Should().Be(DomainConstants.MaxPageSize);
            result.TotalCount.Should().Be(25);
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetUsersAsync_OnTheSecondPage_ShouldReturnTheRemainingUsers()
        {
            for (var i = 0; i < 25; i++)
                await _host.GivenUserAsync(Roles.Cliente, $"cliente{i:00}");

            var result = await _service.GetUsersAsync(2, 20, StatusFilter.Todos);

            result.Items.Should().HaveCount(5);
            result.Page.Should().Be(2);
        }

        [Fact]
        public async Task GetUsersByRoleAsync_WithTheCommerceRole_ShouldReturnAnEmptyPage()
        {
            await _host.GivenUserAsync(Roles.Comercio, "comercio01");

            var result = await _service.GetUsersByRoleAsync(Roles.Comercio, 1, 20);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetUsersByRoleAsync_ShouldReturnOnlyTheRequestedRole()
        {
            await _host.GivenUserAsync(Roles.Administrador, "admin01");
            await _host.GivenUserAsync(Roles.Cajero, "cajero01");
            await _host.GivenUserAsync(Roles.Cajero, "cajero02");

            var result = await _service.GetUsersByRoleAsync(Roles.Cajero, 1, 20);

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(user => user.TypeUser == Roles.Cajero);
        }

        [Fact]
        public async Task GetRolesAsync_ShouldExcludeTheCommerceRole()
        {
            var roles = await _service.GetRolesAsync();

            roles.Should().BeEquivalentTo(
                nameof(Roles.Administrador), nameof(Roles.Cajero), nameof(Roles.Cliente));
        }

        [Fact]
        public async Task GetCommerceUsersAsync_ShouldReturnOnlyCommerceUsers()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            await _host.GivenUserAsync(Roles.Comercio, "comercio01");

            var result = await _service.GetCommerceUsersAsync(1, 20);

            result.Items.Should().ContainSingle().Which.TypeUser.Should().Be(Roles.Comercio);
        }

        // ─── Cambio de estado ────────────────────────────────────────────────

        //El administrador autenticado no puede modificar el estado de su propia cuenta
        [Fact]
        public async Task ToggleUserAsync_OnTheCurrentUserOwnAccount_ShouldBeRejected()
        {
            var admin = await _host.GivenUserAsync(Roles.Administrador, "admin01");

            var response = await _service.ToggleUserAsync(admin.Id, admin.Id);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("No puede modificar el estado de su propia cuenta.");
            (await _host.UserManager.FindByIdAsync(admin.Id))!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ToggleUserAsync_WithAnUnknownUser_ShouldReportItAsNotFound()
        {
            var response = await _service.ToggleUserAsync("no-existe", "admin-1");

            response.HasError.Should().BeTrue();
            response.NotFound.Should().BeTrue();
            response.Error.Should().Be("El usuario seleccionado no existe.");
        }

        [Fact]
        public async Task ToggleUserAsync_OnAnActiveUser_ShouldDeactivateIt()
        {
            var admin = await _host.GivenUserAsync(Roles.Administrador, "admin01");
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var response = await _service.ToggleUserAsync(client.Id, admin.Id);

            response.HasError.Should().BeFalse();
            var updated = await _host.UserManager.FindByIdAsync(client.Id);
            updated!.IsActive.Should().BeFalse();
            updated.EmailConfirmed.Should().BeFalse();
        }

        [Fact]
        public async Task SetUserStatusAsync_WithTrue_ShouldActivateAnInactiveUser()
        {
            var admin = await _host.GivenUserAsync(Roles.Administrador, "admin01");
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01", isActive: false);

            var response = await _service.SetUserStatusAsync(client.Id, true, admin.Id);

            response.HasError.Should().BeFalse();
            (await _host.UserManager.FindByIdAsync(client.Id))!.IsActive.Should().BeTrue();
        }

        // ─── Consultas ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnTheRoleAndTheStateOfTheUser()
        {
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var detail = await _service.GetUserByIdAsync(client.Id);

            detail.Should().NotBeNull();
            detail!.TypeUser.Should().Be(Roles.Cliente);
            detail.State.Should().BeTrue();
            detail.IsClient.Should().BeTrue();
            detail.UserName.Should().Be("cliente01");
        }

        [Fact]
        public async Task GetUserByIdAsync_WithACommerceUser_ShouldReturnNull()
        {
            var commerce = await _host.GivenUserAsync(Roles.Comercio, "comercio01");

            (await _service.GetUserByIdAsync(commerce.Id)).Should().BeNull();
        }

        [Fact]
        public async Task GetRolesByUserAsync_ShouldReturnTheRolesOfTheUser()
        {
            var cashier = await _host.GivenUserAsync(Roles.Cajero, "cajero01");

            var roles = await _service.GetRolesByUserAsync(cashier.Id);

            roles.Should().ContainSingle().Which.Should().Be(nameof(Roles.Cajero));
        }

        [Fact]
        public async Task GetClientBaseDataAsync_WithAUserThatIsNotAClient_ShouldReturnNull()
        {
            var cashier = await _host.GivenUserAsync(Roles.Cajero, "cajero01");

            (await _service.GetClientBaseDataAsync(cashier.Id)).Should().BeNull();
        }

        [Fact]
        public async Task GetClientBaseDataAsync_WithAClient_ShouldReturnIdNameAndLastName()
        {
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var data = await _service.GetClientBaseDataAsync(client.Id);

            data.Should().NotBeNull();
            data!.Id.Should().Be(client.Id);
            data.FirstName.Should().Be("María");
            data.LastName.Should().Be("Gómez");
        }

        //El cajero necesita el titular en una sola cadena para mostrarlo en sus pantallas
        [Fact]
        public async Task GetFullNameByIdAsync_ShouldReturnTheNameAndLastNameInASingleString()
        {
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            (await _service.GetFullNameByIdAsync(client.Id)).Should().Be("María Gómez");
        }

        //También resuelve titulares que no son clientes: el cajero consulta por producto
        [Fact]
        public async Task GetFullNameByIdAsync_WithAUserThatIsNotAClient_ShouldStillReturnTheFullName()
        {
            var cashier = await _host.GivenUserAsync(Roles.Cajero, "cajero01", firstName: "Juan", lastName: "Pérez");

            (await _service.GetFullNameByIdAsync(cashier.Id)).Should().Be("Juan Pérez");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("no-existe")]
        public async Task GetFullNameByIdAsync_WithoutAValidUser_ShouldReturnNull(string userId)
        {
            (await _service.GetFullNameByIdAsync(userId)).Should().BeNull();
        }

        [Fact]
        public async Task ValidateUserExistsByIdCardAsync_ShouldReportExistenceAndCurrentState()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", isActive: false, idCard: "00187654321");

            var result = await _service.ValidateUserExistsByIdCardAsync("00187654321");

            result.Exists.Should().BeTrue();
            result.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUserExistsByIdCardAsync_WithACommerceUser_ShouldReportItAsNonExistent()
        {
            await _host.GivenUserAsync(Roles.Comercio, "comercio01", idCard: "10199999999");

            var result = await _service.ValidateUserExistsByIdCardAsync("10199999999");

            result.Exists.Should().BeFalse();
        }

        [Fact]
        public async Task GetActiveClientsAsync_ShouldReturnOnlyActiveUsersWithTheClientRole()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            await _host.GivenUserAsync(Roles.Cliente, "cliente02", isActive: false);
            await _host.GivenUserAsync(Roles.Cajero, "cajero01");

            var clients = await _service.GetActiveClientsAsync();

            clients.Should().ContainSingle().Which.FullName.Should().Be("María Gómez");
        }

        [Fact]
        public async Task GetActiveClientIdsAsync_ShouldReturnOnlyTheIdentifiers()
        {
            var client = await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            await _host.GivenUserAsync(Roles.Cliente, "cliente02", isActive: false);

            var ids = await _service.GetActiveClientIdsAsync();

            ids.Should().ContainSingle().Which.Should().Be(client.Id);
        }

        [Fact]
        public async Task GetClientByIdCardAsync_WithAnInactiveClient_ShouldReturnNull()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", isActive: false, idCard: "00187654321");

            (await _service.GetClientByIdCardAsync("00187654321")).Should().BeNull();
        }

        [Fact]
        public async Task GetClientByIdCardAsync_WithAnActiveClient_ShouldReturnItsSummary()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", idCard: "00187654321");

            var summary = await _service.GetClientByIdCardAsync("00187654321");

            summary.Should().NotBeNull();
            summary!.IDCARD.Should().Be("00187654321");
            summary.FullName.Should().Be("María Gómez");
        }

        // ─── Edición ─────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserAsync_WithAnUnknownUser_ShouldReportItAsNotFound()
        {
            var response = await _service.UpdateUserAsync("no-existe", BuildEdit("no-existe"));

            response.NotFound.Should().BeTrue();
            response.Error.Should().Be("El usuario seleccionado no existe.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithAnEmailThatBelongsToAnotherUser_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", email: "ocupado@artemisbank.com");
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente02");

            var dto = BuildEdit(target.Id, email: "ocupado@artemisbank.com");
            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe otro usuario registrado con este correo electrónico.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithAUserNameThatBelongsToAnotherUser_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "ocupado");
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente02");

            var dto = BuildEdit(target.Id, userName: "ocupado");
            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe otro usuario registrado con este nombre de usuario.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithAnIdCardThatBelongsToAnotherUser_ShouldReportAConflict()
        {
            await _host.GivenUserAsync(Roles.Cliente, "cliente01", idCard: "00187654321");
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente02", idCard: "00100000009");

            var dto = BuildEdit(target.Id, idCard: "00187654321");
            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.Conflict.Should().BeTrue();
            response.Error.Should().Be("Ya existe otro usuario registrado con esta cédula.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithANewPasswordAndNoConfirmation_ShouldAskForTheConfirmation()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var dto = BuildEdit(target.Id);
            dto.NewPassword = "NuevaClave1*";

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("Debe confirmar la nueva contraseña.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithMismatchedPasswords_ShouldRejectTheChange()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var dto = BuildEdit(target.Id);
            dto.NewPassword = "NuevaClave1*";
            dto.ConfirmNewPassword = "OtraClave2*";

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("La contraseña y la confirmación de contraseña deben coincidir.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithANegativeAdditionalAmount_ShouldRejectTheChange()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var dto = BuildEdit(target.Id);
            dto.AdditionalAmount = -1m;

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("El monto adicional no puede ser negativo.");
        }

        //Regresión: la edición llegó a sobrescribir el nombre de usuario con el correo,
        //lo que dejaba al usuario sin poder iniciar sesión.
        [Fact]
        public async Task UpdateUserAsync_ShouldNotOverwriteTheUserNameWithTheEmail()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            var dto = BuildEdit(target.Id, userName: "cliente01", email: "nuevo.correo@artemisbank.com");
            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeFalse();
            var updated = await _host.UserManager.FindByIdAsync(target.Id);
            updated!.UserName.Should().Be("cliente01");
            updated.Email.Should().Be("nuevo.correo@artemisbank.com");
        }

        [Fact]
        public async Task UpdateUserAsync_WithAnEmptyPassword_ShouldKeepTheCurrentOne()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            await _service.UpdateUserAsync(target.Id, BuildEdit(target.Id));

            var updated = await _host.UserManager.FindByIdAsync(target.Id);
            (await _host.UserManager.CheckPasswordAsync(updated!, "Clave123*")).Should().BeTrue();
        }

        //Todo monto adicional mayor que cero se suma a la cuenta principal y se registra
        //como una transacción de tipo Crédito.
        [Fact]
        public async Task UpdateUserAsync_WithAnAdditionalAmount_ShouldCreditThePrimaryAccount()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            var account = GivenPrimaryAccount(target.Id, balance: 5000m);

            Transaction? registered = null;
            _transactionRepository.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => registered = t)
                .ReturnsAsync((Transaction t) => t);

            var dto = BuildEdit(target.Id);
            dto.AdditionalAmount = 12000m;

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeFalse();
            account.Balance.Should().Be(17000m);

            registered.Should().NotBeNull();
            registered!.TransactionType.Should().Be(TransactionType.Credito);
            registered.Amount.Should().Be(12000m);
            registered.SavingsAccountId.Should().Be(account.Id);
            _savingsAccountsRepository.Verify(r => r.UpdateAsync(account), Times.Once);
            _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WithAnAdditionalAmountOfZero_ShouldNotTouchTheBalance()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");
            var account = GivenPrimaryAccount(target.Id, balance: 5000m);

            var dto = BuildEdit(target.Id);
            dto.AdditionalAmount = 0m;

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeFalse();
            account.Balance.Should().Be(5000m);
            _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }

        //El monto adicional solo aplica a clientes
        [Fact]
        public async Task UpdateUserAsync_WithAnAdditionalAmountOnACashier_ShouldBeRejected()
        {
            var cashier = await _host.GivenUserAsync(Roles.Cajero, "cajero01");

            var dto = BuildEdit(cashier.Id, userName: "cajero01");
            dto.AdditionalAmount = 500m;

            var response = await _service.UpdateUserAsync(cashier.Id, dto);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("El monto adicional solo puede asignarse a usuarios con rol Cliente.");
        }

        [Fact]
        public async Task UpdateUserAsync_WithoutAPrimaryAccount_ShouldReportThatItWasNotFound()
        {
            var target = await _host.GivenUserAsync(Roles.Cliente, "cliente01");

            _savingsAccountsRepository.Setup(r => r.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((SavingsAccount?)null);

            var dto = BuildEdit(target.Id);
            dto.AdditionalAmount = 100m;

            var response = await _service.UpdateUserAsync(target.Id, dto);

            response.HasError.Should().BeTrue();
            response.Error.Should().Be("No se encontró una cuenta de ahorro principal activa para asignar el monto adicional.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static EditUserDto BuildEdit(
            string id,
            string userName = "cliente01",
            string email = "cliente01@artemisbank.com",
            string idCard = "00100000009")
        {
            return new EditUserDto
            {
                Id = id,
                Name = "María",
                LastName = "Gómez",
                IDCARD = idCard,
                Email = email,
                UserName = userName
            };
        }

        private SavingsAccount GivenPrimaryAccount(string customerId, decimal balance)
        {
            var account = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "500000001",
                CustomerId = customerId,
                Balance = balance,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = DomainConstants.SystemUserId
            };

            _savingsAccountsRepository.Setup(r => r.GetFirstAsync(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                    It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(account);
            _savingsAccountsRepository.Setup(r => r.UpdateAsync(It.IsAny<SavingsAccount>())).ReturnsAsync(true);

            return account;
        }

        public void Dispose() => _host.Dispose();
    }
}
