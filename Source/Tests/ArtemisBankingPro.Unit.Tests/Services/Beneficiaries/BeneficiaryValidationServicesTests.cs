using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.Services.Beneficiaries;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Beneficiaries
{
    public sealed class BeneficiaryValidationServicesTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepositoryMock;
        private readonly Mock<ILogger<BeneficiaryValidationServices>> _loggerMock;
        private readonly BeneficiaryValidationServices _validationServices;

        public BeneficiaryValidationServicesTests()
        {
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _beneficiaryRepositoryMock = new Mock<IBeneficiaryRepository>();
            _loggerMock = new Mock<ILogger<BeneficiaryValidationServices>>();

            _validationServices = new BeneficiaryValidationServices(
                _savingsAccountsRepositoryMock.Object,
                _beneficiaryRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("12345678")]
        [InlineData("1234567890")]
        [InlineData("abcde1234")]
        public async Task ValidateCreationAsync_WithInvalidFormatAccountNumber_ShouldReturnAccountNotFound(string invalidAccountNumber)
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = invalidAccountNumber
            };

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AccountNotFound);
        }

        [Fact]
        public async Task ValidateCreationAsync_WithNonExistentAccount_ShouldReturnAccountNotFound()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "999999999"
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync((SavingsAccount)null!);

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AccountNotFound);
        }

        [Fact]
        public async Task ValidateCreationAsync_WithCanceledAccount_ShouldReturnAccountCanceled()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "111111111"
            };

            var account = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "111111111",
                CustomerId = "other-1",
                Status = SavingsAccountStatus.Cancelada,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AccountCanceled);
        }

        [Fact]
        public async Task ValidateCreationAsync_WithOwnAccount_ShouldReturnOwnAccount()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "222222222"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "222222222",
                CustomerId = "owner-1",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.OwnAccount);
        }

        [Fact]
        public async Task ValidateCreationAsync_WithAlreadyRegisteredAccount_ShouldReturnAlreadyRegistered()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "333333333"
            };

            var account = new SavingsAccount
            {
                Id = 3,
                AccountNumber = "333333333",
                CustomerId = "other-2",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            _beneficiaryRepositoryMock.Setup(r => r.ExistElementByConsult(
                It.IsAny<Expression<Func<Beneficiary, bool>>>()
            )).ReturnsAsync(true);

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.AlreadyRegistered);
        }

        [Fact]
        public async Task ValidateCreationAsync_WithValidAccount_ShouldReturnSuccessWithAccount()
        {
            var dto = new SaveBeneficiaryDto
            {
                OwnerClientId = "owner-1",
                AccountNumber = "444444444"
            };

            var account = new SavingsAccount
            {
                Id = 4,
                AccountNumber = "444444444",
                CustomerId = "other-3",
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Secundaria,
                CreateByUserId = "system",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _savingsAccountsRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<SavingsAccount, bool>>>(),
                It.IsAny<Expression<Func<SavingsAccount, object>>[]>()
            )).ReturnsAsync(account);

            _beneficiaryRepositoryMock.Setup(r => r.ExistElementByConsult(
                It.IsAny<Expression<Func<Beneficiary, bool>>>()
            )).ReturnsAsync(false);

            var result = await _validationServices.ValidateCreationAsync(dto);

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(account);
        }

        [Fact]
        public async Task ValidateDeactivationAsync_WithNonExistentBeneficiary_ShouldReturnBeneficiaryNotFound()
        {
            _beneficiaryRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<Beneficiary, bool>>>(),
                It.IsAny<Expression<Func<Beneficiary, object>>[]>()
            )).ReturnsAsync((Beneficiary)null!);

            var result = await _validationServices.ValidateDeactivationAsync(10, "owner-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(BeneficiaryError.BeneficiaryNotFound);
        }

        [Fact]
        public async Task ValidateDeactivationAsync_WithValidBeneficiary_ShouldReturnSuccessWithBeneficiary()
        {
            var beneficiary = new Beneficiary
            {
                Id = 10,
                OwnerClientId = "owner-1",
                BeneficiarySavingsAccountId = 5,
                BeneficiaryAccountNumber = "555555555",
                IsActive = true,
                CreateByUserId = "owner-1",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _beneficiaryRepositoryMock.Setup(r => r.GetFirstAsync(
                It.IsAny<Expression<Func<Beneficiary, bool>>>(),
                It.IsAny<Expression<Func<Beneficiary, object>>[]>()
            )).ReturnsAsync(beneficiary);

            var result = await _validationServices.ValidateDeactivationAsync(10, "owner-1");

            result.IsValid.Should().BeTrue();
            result.Value.Should().Be(beneficiary);
        }
    }
}
