using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts.SavingsAccountsValidate;
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
        private readonly SavingsAccountsValidateServices _sut;

        private const string CustomerId = "3f2a1c9e-0000-4a2b-9c1d-8e7f6a5b4c3d";

        public SavingsAccountsValidateServicesTests()
            => _sut = new SavingsAccountsValidateServices(
                _savingsAccountsRepository.Object,
                NullLogger<SavingsAccountsValidateServices>.Instance);

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

        [Fact]
        public async Task ValidateCustomerAccountsQueryAsync_WithoutIdCard_ShouldSucceed()
        {
            var result = await _sut.ValidateCustomerAccountsQueryAsync(new SavingsAccountFilterDto());

            result.IsValid.Should().BeTrue();
        }
        #endregion
    }
}
