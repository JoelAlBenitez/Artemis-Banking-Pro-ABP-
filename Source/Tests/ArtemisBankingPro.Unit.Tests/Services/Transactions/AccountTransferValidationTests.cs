using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using ArtemisBankingPro.Core.Application.Contracts.Users.Management;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class AccountTransferValidationTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ILogger<TransactionsValidationServices>> _loggerMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly TransactionsValidationServices _validationService;

        public AccountTransferValidationTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _loggerMock = new Mock<ILogger<TransactionsValidationServices>>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            _validationService = new TransactionsValidationServices(
                _savingsAccountRepositoryMock.Object,
                new Mock<ICreditCardsRepository>().Object,
                new Mock<ILoansRepository>().Object,
                new Mock<IBeneficiaryRepository>().Object,
                new Mock<ILoanInstallmentRepository>().Object,
                _loggerMock.Object,
                _userManagementServiceMock.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task ValidateAccountTransferAsync_WithAmountLowerOrEqualToZero_ShouldReturnTransferInvalidAmount(decimal amount)
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = amount };
            var clientId = "client-123";

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.TransferInvalidAmount);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithLessThanTwoActiveAccounts_ShouldReturnMinTwoAccountsRequired()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = 100 };
            var clientId = "client-123";

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(1);

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.MinTwoAccountsRequired);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithSameSourceAndDestination_ShouldReturnTransferSameAccount()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 1, Amount = 100 };
            var clientId = "client-123";

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(2);

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.TransferSameAccount);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithInvalidSourceAccount_ShouldReturnOriginAccountNotFound()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = 100 };
            var clientId = "client-123";

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(2);

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.Is<Expression<Func<SavingsAccount, bool>>>(exp => exp.ToString().Contains("SourceAccountId") || exp.ToString().Contains("Id == 1")), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((SavingsAccount)null!);

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.OriginAccountNotFound);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithInvalidDestinationAccount_ShouldReturnDestinationAccountNotFound()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = 100 };
            var clientId = "client-123";

            var sourceAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "ACC-01",
                CustomerId = clientId,
                Balance = 500,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(2);

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((Expression<Func<SavingsAccount, bool>> predicate, Expression<Func<SavingsAccount, object>>[] includes) =>
                {
                    var compiled = predicate.Compile();
                    if (compiled(sourceAccount)) return sourceAccount;
                    return null!;
                });

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.DestinationAccountNotFound);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithInsufficientFunds_ShouldReturnTransferInsufficientFunds()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = 1000 };
            var clientId = "client-123";

            var sourceAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "ACC-01",
                CustomerId = clientId,
                Balance = 500,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "ACC-02",
                CustomerId = clientId,
                Balance = 100,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(2);

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((Expression<Func<SavingsAccount, bool>> predicate, Expression<Func<SavingsAccount, object>>[] includes) =>
                {
                    var compiled = predicate.Compile();
                    if (compiled(sourceAccount)) return sourceAccount;
                    if (compiled(destAccount)) return destAccount;
                    return null!;
                });

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.TransferInsufficientFunds);
        }

        [Fact]
        public async Task ValidateAccountTransferAsync_WithValidInputs_ShouldReturnSuccess()
        {
            var dto = new AccountTransferDto { SourceAccountId = 1, DestinationAccountId = 2, Amount = 100 };
            var clientId = "client-123";

            var sourceAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "ACC-01",
                CustomerId = clientId,
                Balance = 500,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "ACC-02",
                CustomerId = clientId,
                Balance = 100,
                Status = SavingsAccountStatus.Activa,
                AccountType = SavingsAccountType.Principal,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            _savingsAccountRepositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>()))
                .ReturnsAsync(2);

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((Expression<Func<SavingsAccount, bool>> predicate, Expression<Func<SavingsAccount, object>>[] includes) =>
                {
                    var compiled = predicate.Compile();
                    if (compiled(sourceAccount)) return sourceAccount;
                    if (compiled(destAccount)) return destAccount;
                    return null!;
                });

            var result = await _validationService.ValidateAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Origin.Should().Be(sourceAccount);
            result.Value.Destination.Should().Be(destAccount);
        }
    }
}
