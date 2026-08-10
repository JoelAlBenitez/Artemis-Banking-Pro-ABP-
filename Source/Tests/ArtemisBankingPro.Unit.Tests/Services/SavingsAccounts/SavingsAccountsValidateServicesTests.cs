using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts.SavingsAccountsValidate;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Application.DTOs.Users;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.SavingsAccounts
{
    
    public sealed class SavingsAccountsValidateServicesTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepository = new();
        private readonly Mock<IUserManagementService> _userManagementService = new();
        private readonly Mock<ICurrentUserService> _currentUserService = new();
        private readonly SavingsAccountsValidateServices _sut;

        private const string CustomerId = "3f2a1c9e-0000-4a2b-9c1d-8e7f6a5b4c3d";
        private const string AdminUserId = "8b1d4f60-1111-4c3a-9d2e-7f6a5b4c3d2e";

        public SavingsAccountsValidateServicesTests()
        {
            //Cliente existente y activo por defecto: cada prueba que valide lo contrario lo cambia
            SetupCustomer(exists: true, isActive: true);
            SetupAdministratorInSession(AdminUserId);

            _sut = new SavingsAccountsValidateServices(
                _savingsAccountsRepository.Object,
                _userManagementService.Object,
                _currentUserService.Object,
                NullLogger<SavingsAccountsValidateServices>.Instance);
        }

        private void SetupCustomer(bool exists, bool isActive)
            => _userManagementService
                .Setup(service => service.ValidateUserExistsByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserExistenceDto { Exists = exists, IsActive = isActive });

        private void SetupAdministratorInSession(string? userId, bool isAdmin = true)
        {
            _currentUserService.Setup(service => service.UserId).Returns(userId);
            _currentUserService
                .Setup(service => service.IsInRole(Roles.Administrador.ToString()))
                .Returns(isAdmin);
        }

        private void SetupPrimaryAccountExists(bool exists)
            => _savingsAccountsRepository
                .Setup(repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(exists);

        private static SavingsAccount BuildAccount(
            SavingsAccountType type = SavingsAccountType.Secundaria,
            SavingsAccountStatus status = SavingsAccountStatus.Activa,
            decimal balance = 0m)
            => new()
            {
                Id = 10,
                AccountNumber = "500000001",
                CustomerId = CustomerId,
                Balance = balance,
                AccountType = type,
                Status = status,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "SYSTEM"
            };

        #region ValidateCustomerSelectionAsync
        [Fact]
        public async Task ValidateCustomerSelectionAsync_WithoutCustomer_ShouldFailWithCustomerRequired()
        {
            var result = await _sut.ValidateCustomerSelectionAsync(string.Empty);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.CustomerRequired);
        }

        [Fact]
        public async Task ValidateCustomerSelectionAsync_WithANonExistentCustomer_ShouldFail()
        {
            SetupCustomer(exists: false, isActive: false);

            var result = await _sut.ValidateCustomerSelectionAsync(CustomerId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsCustomerByIdCard);
        }

        //Regla del documento: solo se asignan cuentas a clientes activos.
        [Fact]
        public async Task ValidateCustomerSelectionAsync_WithAnInactiveCustomer_ShouldFail()
        {
            SetupCustomer(exists: true, isActive: false);

            var result = await _sut.ValidateCustomerSelectionAsync(CustomerId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.CustomerIsNotActive);
            _savingsAccountsRepository.Verify(
                repository => repository.ExistElementByConsult(
                    It.IsAny<Expression<Func<SavingsAccount, bool>>>()),
                Times.Never);
        }

        [Fact]
        public async Task ValidateCustomerSelectionAsync_WithoutActivePrimaryAccount_ShouldFail()
        {
            SetupPrimaryAccountExists(false);

            var result = await _sut.ValidateCustomerSelectionAsync(CustomerId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.CustomerWithoutActivePrimaryAccount);
        }

        [Fact]
        public async Task ValidateCustomerSelectionAsync_WithActivePrimaryAccount_ShouldSucceed()
        {
            SetupPrimaryAccountExists(true);

            var result = await _sut.ValidateCustomerSelectionAsync(CustomerId);

            result.IsValid.Should().BeTrue();
        }
        #endregion

        #region ValidateAssignmentAsync
        [Fact]
        public async Task ValidateAssignmentAsync_WithNullDto_ShouldFailWithDataInvalid()
        {
            var result = await _sut.ValidateAssignmentAsync(null!);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(GeneralError.DataInvalid);
        }

        [Fact]
        public async Task ValidateAssignmentAsync_WithNegativeBalance_ShouldFail()
        {
            SetupPrimaryAccountExists(true);

            var result = await _sut.ValidateAssignmentAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = -1m });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NegativeInitialBalance);
        }

        //Regla explícita del módulo: RD$0.00 es un balance inicial válido.
        [Fact]
        public async Task ValidateAssignmentAsync_WithZeroBalance_ShouldSucceed()
        {
            SetupPrimaryAccountExists(true);

            var result = await _sut.ValidateAssignmentAsync(
                new SavingsAccountAssignmentDto { CustomerId = CustomerId, InitialBalance = 0m });

            result.IsValid.Should().BeTrue();
        }
        #endregion

        #region ValidateActiveSavingsAccountAsync
        [Fact]
        public async Task ValidateActiveSavingsAccountAsync_WithNonExistentAccount_ShouldFail()
        {
            _savingsAccountsRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((SavingsAccount)null!);

            var result = await _sut.ValidateActiveSavingsAccountAsync(99);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsSavingsAccount);
        }

        [Fact]
        public async Task ValidateActiveSavingsAccountAsync_WithCancelledAccount_ShouldFail()
        {
            _savingsAccountsRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(BuildAccount(status: SavingsAccountStatus.Cancelada));

            var result = await _sut.ValidateActiveSavingsAccountAsync(10);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.SavingsAccountAlreadyCancelled);
        }
        #endregion

        #region ValidateCancellationAsync
        [Fact]
        public async Task ValidateCancellationAsync_WithPrimaryAccount_ShouldFail()
        {
            _savingsAccountsRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(BuildAccount(SavingsAccountType.Principal));

            var result = await _sut.ValidateCancellationAsync(10);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.PrimaryAccountCannotBeCancelled);
        }

        [Fact]
        public async Task ValidateCancellationAsync_WithoutPrimaryAccountToReceiveFunds_ShouldFail()
        {
            _savingsAccountsRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(BuildAccount());

            SetupPrimaryAccountExists(false);

            var result = await _sut.ValidateCancellationAsync(10);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.WithoutPrimaryAccountToReceiveFunds);
        }

        [Fact]
        public async Task ValidateCancellationAsync_WithSecondaryActiveAccount_ShouldSucceedAndReturnTheEntity()
        {
            var account = BuildAccount(balance: 1500m);

            _savingsAccountsRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(account);

            SetupPrimaryAccountExists(true);

            var result = await _sut.ValidateCancellationAsync(10);

            result.IsValid.Should().BeTrue();
            result.Value.Should().BeSameAs(account);
        }
        #endregion

        #region ValidateCustomerAccountsQueryAsync
        [Fact]
        public async Task ValidateCustomerAccountsQueryAsync_WithNullFilter_ShouldFailWithDataInvalid()
        {
            var result = await _sut.ValidateCustomerAccountsQueryAsync(null!);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(GeneralError.DataInvalid);
        }

        //Sin cédula el listado va sin filtro de cliente: no se consulta Identity.
        [Fact]
        public async Task ValidateCustomerAccountsQueryAsync_WithoutIdCard_ShouldSucceedWithoutCustomerId()
        {
            var result = await _sut.ValidateCustomerAccountsQueryAsync(new SavingsAccountFilterDto());

            result.IsValid.Should().BeTrue();
            result.Value.Should().BeNull();
            _userManagementService.Verify(
                service => service.GetClientByIdCardAsync(It.IsAny<string>()), Times.Never);
        }

        //La cédula se traduce al Id del cliente: es la clave con la que se filtran las cuentas.
        [Fact]
        public async Task ValidateCustomerAccountsQueryAsync_WithAKnownIdCard_ShouldReturnTheCustomerId()
        {
            _userManagementService
                .Setup(service => service.GetClientByIdCardAsync("40200000001"))
                .ReturnsAsync(new ClientSummaryDto
                {
                    Id = CustomerId,
                    IDCARD = "40200000001",
                    FullName = "María Gómez",
                    Email = "maria.gomez@artemis.com"
                });

            var result = await _sut.ValidateCustomerAccountsQueryAsync(
                new SavingsAccountFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(CustomerId);
        }

        [Fact]
        public async Task ValidateCustomerAccountsQueryAsync_WithAnUnknownIdCard_ShouldFail()
        {
            _userManagementService
                .Setup(service => service.GetClientByIdCardAsync(It.IsAny<string>()))
                .ReturnsAsync((ClientSummaryDto?)null);

            var result = await _sut.ValidateCustomerAccountsQueryAsync(
                new SavingsAccountFilterDto { IdCard = "40200000001" });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.NonExistsCustomerByIdCard);
        }
        #endregion

        #region ValidateAdministratorInSession
        [Fact]
        public void ValidateAdministratorInSession_WithAnAuthenticatedAdministrator_ShouldReturnItsId()
        {
            var result = _sut.ValidateAdministratorInSession();

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(AdminUserId);
        }

        [Fact]
        public void ValidateAdministratorInSession_WithoutSession_ShouldFail()
        {
            SetupAdministratorInSession(null);

            var result = _sut.ValidateAdministratorInSession();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.AdminUserRequired);
        }

        //Un usuario autenticado que no es administrador tampoco puede firmar la operación.
        [Fact]
        public void ValidateAdministratorInSession_WithoutTheAdministratorRole_ShouldFail()
        {
            SetupAdministratorInSession(AdminUserId, isAdmin: false);

            var result = _sut.ValidateAdministratorInSession();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(SavingsAccountError.AdminUserRequired);
        }
        #endregion
    }
}
