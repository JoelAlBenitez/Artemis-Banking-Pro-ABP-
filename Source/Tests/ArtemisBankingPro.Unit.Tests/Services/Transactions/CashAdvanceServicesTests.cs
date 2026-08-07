using System;
using System.Collections.Generic;
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
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public sealed class CashAdvanceServicesTests
    {
        private readonly Mock<ICashAdvanceValidationServices> _validationServicesMock;
        private readonly Mock<ICashAdvanceRepository> _cashAdvanceRepositoryMock;
        private readonly Mock<ICreditCardsRepository> _creditCardsRepositoryMock;
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountsRepositoryMock;
        private readonly Mock<ICardConsumptionRepository> _cardConsumptionRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<CashAdvanceServices>> _loggerMock;
        private readonly CashAdvanceServices _services;

        public CashAdvanceServicesTests()
        {
            _validationServicesMock = new Mock<ICashAdvanceValidationServices>();
            _cashAdvanceRepositoryMock = new Mock<ICashAdvanceRepository>();
            _creditCardsRepositoryMock = new Mock<ICreditCardsRepository>();
            _savingsAccountsRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _cardConsumptionRepositoryMock = new Mock<ICardConsumptionRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _emailServicesMock = new Mock<IEmailServices>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<CashAdvanceServices>>();

            _services = new CashAdvanceServices(
                _validationServicesMock.Object,
                _cashAdvanceRepositoryMock.Object,
                _creditCardsRepositoryMock.Object,
                _savingsAccountsRepositoryMock.Object,
                _cardConsumptionRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _emailServicesMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task ProcessCashAdvanceAsync_WithValidationFailure_ShouldReturnFailure()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                Amount = 100
            };

            _validationServicesMock.Setup(r => r.ValidateCashAdvanceAsync(It.IsAny<CashAdvanceRequestDto>(), It.IsAny<string>()))
                .ReturnsAsync(ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.InsufficientCredit));

            var result = await _services.ProcessCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(CashAdvanceError.InsufficientCredit);
        }

        [Fact]
        public async Task ProcessCashAdvanceAsync_WithSuccessfulValidation_ShouldCommitOperationAndReturnSuccess()
        {
            var dto = new CashAdvanceRequestDto
            {
                CreditCardId = 1,
                SavingsAccountId = 2,
                Amount = 100
            };

            var card = new CreditCard
            {
                Id = 1,
                CardNumber = "1234567890123456",
                LastFourDigits = "3456",
                CustomerId = "client-1",
                CreditLimit = 1000,
                OwedAmount = 100,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
                CvcHash = "hash",
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin"
            };

            var account = new SavingsAccount
            {
                Id = 2,
                AccountNumber = "123456789",
                CustomerId = "client-1",
                Balance = 500,
                AccountType = SavingsAccountType.Secundaria,
                Status = SavingsAccountStatus.Activa,
                CreateByUserId = "admin",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _validationServicesMock.Setup(r => r.ValidateCashAdvanceAsync(It.IsAny<CashAdvanceRequestDto>(), It.IsAny<string>()))
                .ReturnsAsync(ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Success((card, account, 6.25m, 106.25m)));

            _creditCardsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CreditCard>()))
                .ReturnsAsync(true);

            _savingsAccountsRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SavingsAccount>()))
                .ReturnsAsync(true);

            _cardConsumptionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CardConsumption>()))
                .ReturnsAsync((CardConsumption c) => c);

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _cashAdvanceRepositoryMock.Setup(r => r.AddAsync(It.IsAny<CashAdvance>()))
                .ReturnsAsync((CashAdvance ca) => ca);

            _cashAdvanceRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            _emailServicesMock.Setup(r => r.SendNotification(It.IsAny<MessageDto>()))
                .ReturnsAsync(true);

            _mapperMock.Setup(m => m.Map<CashAdvanceDto>(It.IsAny<CashAdvance>()))
                .Returns((CashAdvance ca) => new CashAdvanceDto
                {
                    RequestedAmount = ca.RequestedAmount,
                    InterestAmount = ca.InterestAmount,
                    TotalCharged = ca.TotalCharged,
                    CardLastFourDigits = "3456",
                    AccountLastFourDigits = "6789",
                    CreatedAt = ca.CreatedAt
                });

            var result = await _services.ProcessCashAdvanceAsync(dto, "client-1");

            result.IsValid.Should().BeTrue();
            result.Value!.RequestedAmount.Should().Be(100);
            result.Value.InterestAmount.Should().Be(6.25m);
            result.Value.TotalCharged.Should().Be(106.25m);
            result.Value.CardLastFourDigits.Should().Be("3456");
            result.Value.AccountLastFourDigits.Should().Be("6789");

            card.OwedAmount.Should().Be(206.25m);
            account.Balance.Should().Be(600m);

            _creditCardsRepositoryMock.Verify(r => r.UpdateAsync(card), Times.Once);
            _savingsAccountsRepositoryMock.Verify(r => r.UpdateAsync(account), Times.Once);
            _cardConsumptionRepositoryMock.Verify(r => r.AddAsync(It.Is<CardConsumption>(c => 
                c.CreditCardId == 1 &&
                c.Status == ConsumptionStatus.Aprobado &&
                c.Amount == 106.25m
            )), Times.Once);
            _transactionRepositoryMock.Verify(r => r.AddAsync(It.Is<Transaction>(t => 
                t.SavingsAccountId == 2 &&
                t.TransactionType == TransactionType.Credito &&
                t.Amount == 100m
            )), Times.Once);
            _cashAdvanceRepositoryMock.Verify(r => r.AddAsync(It.Is<CashAdvance>(ca => 
                ca.CreditCardId == 1 &&
                ca.SavingsAccountId == 2 &&
                ca.RequestedAmount == 100 &&
                ca.TotalCharged == 106.25m
            )), Times.Once);
            _cashAdvanceRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _emailServicesMock.Verify(r => r.SendNotification(It.IsAny<MessageDto>()), Times.Once);
        }
    }
}
