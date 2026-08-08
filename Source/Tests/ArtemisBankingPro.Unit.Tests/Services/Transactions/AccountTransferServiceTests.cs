using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
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
using Xunit;

using ArtemisBankingPro.Core.Application.Contracts.Users.Management;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class AccountTransferServiceTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<ITransactionsValidationServices> _validationServicesMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TransactionService>> _loggerMock;
        private readonly Mock<IUserManagementService> _userManagementServiceMock;
        private readonly TransactionService _transactionService;

        public AccountTransferServiceTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _validationServicesMock = new Mock<ITransactionsValidationServices>();
            _emailServicesMock = new Mock<IEmailServices>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TransactionService>>();
            _userManagementServiceMock = new Mock<IUserManagementService>();

            _transactionService = new TransactionService(
                _savingsAccountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                new Mock<IBeneficiaryRepository>().Object,
                new Mock<ICreditCardsRepository>().Object,
                new Mock<ILoansRepository>().Object,
                _validationServicesMock.Object,
                _emailServicesMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _userManagementServiceMock.Object);
        }

        [Fact]
        public async Task ProcessAccountTransferAsync_WithValidationError_ShouldReturnFailureAndRegisterRejectedTransactionIfSourceExists()
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

            _validationServicesMock.Setup(v => v.ValidateAccountTransferAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Failure(TransactionError.TransferInsufficientFunds));

            _savingsAccountRepositoryMock.Setup(r => r.GetFirstAsync(It.IsAny<Expression<Func<SavingsAccount, bool>>>(), It.IsAny<Expression<Func<SavingsAccount, object>>[]>()))
                .ReturnsAsync(sourceAccount);

            _savingsAccountRepositoryMock.Setup(r => r.GetByIdAsync(dto.DestinationAccountId))
                .ReturnsAsync(destAccount);

            var result = await _transactionService.ProcessAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(TransactionError.TransferInsufficientFunds);

            _transactionRepositoryMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
                t.SavingsAccountId == sourceAccount.Id &&
                t.Amount == 1000 &&
                t.Status == TransactionStatus.Rechazada &&
                t.RejectionReason == TransactionError.TransferInsufficientFunds.Description
            )), Times.Once);

            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessAccountTransferAsync_WithSuccess_ShouldDebitSourceCreditDestinationAndReturnSuccess()
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

            _validationServicesMock.Setup(v => v.ValidateAccountTransferAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Success((sourceAccount, destAccount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(true);

            var result = await _transactionService.ProcessAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.EffectiveAmount.Should().Be(100);
            result.Value.Status.Should().Be(TransactionStatus.Aprobada);

            sourceAccount.Balance.Should().Be(400);
            destAccount.Balance.Should().Be(200);

            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(sourceAccount), Times.Once);
            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(destAccount), Times.Once);
            _transactionRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
            _emailServicesMock.Verify(e => e.SendNotification(It.Is<MessageDto>(m =>
                m.To == "client-123@artemis.com" &&
                m.Subject == "Transferencia entre cuentas realizada"
            )), Times.Once);
        }

        [Fact]
        public async Task ProcessAccountTransferAsync_WithSuccessButEmailFailure_ShouldReturnSuccessWithWarning()
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

            _validationServicesMock.Setup(v => v.ValidateAccountTransferAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, SavingsAccount)>.Success((sourceAccount, destAccount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            _emailServicesMock.Setup(e => e.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(false);

            var result = await _transactionService.ProcessAccountTransferAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.WarningMessage.Should().Be("La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.");
        }
    }
}
