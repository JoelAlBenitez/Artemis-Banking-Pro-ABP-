using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class TransactionServiceTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepositoryMock;
        private readonly Mock<ITransactionsValidationServices> _validationServicesMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TransactionService>> _loggerMock;
        private readonly TransactionService _transactionService;

        public TransactionServiceTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _beneficiaryRepositoryMock = new Mock<IBeneficiaryRepository>();
            _validationServicesMock = new Mock<ITransactionsValidationServices>();
            _emailServicesMock = new Mock<IEmailServices>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TransactionService>>();

            _transactionService = new TransactionService(
                _savingsAccountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _beneficiaryRepositoryMock.Object,
                new Mock<ICreditCardsRepository>().Object,
                new Mock<ILoansRepository>().Object,
                _validationServicesMock.Object,
                _emailServicesMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessExpressAsync_WithValidTransaction_ShouldDecreaseOriginIncreaseDestinationAndReturnSuccess()
        {
            var dto = new ExpressTransactionDto
            {
                SourceAccountNumber = "100000001",
                DestinationAccountNumber = "100000002",
                Amount = 500m
            };
            var clientId = "client-123";

            var originAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = clientId,
                Balance = 1000m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "100000002",
                CustomerId = "client-456",
                Balance = 200m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "client-456"
            };

            _validationServicesMock.Setup(v => v.ValidateExpressAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Success((originAccount, destAccount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _transactionService.ProcessExpressAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.EffectiveAmount.Should().Be(500m);
            result.Value.Status.Should().Be("APROBADA");

            originAccount.Balance.Should().Be(500m);
            destAccount.Balance.Should().Be(700m);

            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(originAccount), Times.Once);
            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(destAccount), Times.Once);
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessExpressAsync_WithInsufficientFunds_ShouldRegisterRejectedTransactionAndReturnFailure()
        {
            var dto = new ExpressTransactionDto
            {
                SourceAccountNumber = "100000001",
                DestinationAccountNumber = "100000002",
                Amount = 1500m
            };
            var clientId = "client-123";

            _validationServicesMock.Setup(v => v.ValidateExpressAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.InsufficientFunds));

            var originAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = clientId,
                Balance = 1000m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var destAccount = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "100000002",
                CustomerId = "client-456",
                Balance = 200m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "client-456"
            };

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync((Expression<Func<SavingsAccount, bool>> pred, Expression<Func<SavingsAccount, object>>[] includes) =>
                {
                    var func = pred.Compile();
                    if (func(originAccount)) return originAccount;
                    if (func(destAccount)) return destAccount;
                    return null;
                });

            var result = await _transactionService.ProcessExpressAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.InsufficientFunds);

            _transactionRepositoryMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
                t.SavingsAccountId == originAccount.Id &&
                t.Amount == 1500m &&
                t.Status == TransactionStatus.Rechazada &&
                t.RejectionReason == "Fondos insuficientes"
            )), Times.Once);

            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
