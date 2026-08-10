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
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class PaymentServiceTests
    {
        private readonly Mock<ISavingsAccountsRepository> _savingsAccountRepositoryMock;
        private readonly Mock<ICreditCardsRepository> _creditCardRepositoryMock;
        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<ICardConsumptionRepository> _cardConsumptionRepositoryMock;
        private readonly Mock<ICardPaymentRepository> _cardPaymentRepositoryMock;
        private readonly Mock<ILoanInstallmentRepository> _loanInstallmentRepositoryMock;
        private readonly Mock<ILoansPaymentRepository> _loansPaymentRepositoryMock;
        private readonly Mock<ITransactionsValidationServices> _validationServicesMock;
        private readonly Mock<IEmailServices> _emailServicesMock;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _savingsAccountRepositoryMock = new Mock<ISavingsAccountsRepository>();
            _creditCardRepositoryMock = new Mock<ICreditCardsRepository>();
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _cardConsumptionRepositoryMock = new Mock<ICardConsumptionRepository>();
            _cardPaymentRepositoryMock = new Mock<ICardPaymentRepository>();
            _loanInstallmentRepositoryMock = new Mock<ILoanInstallmentRepository>();
            _loansPaymentRepositoryMock = new Mock<ILoansPaymentRepository>();
            _validationServicesMock = new Mock<ITransactionsValidationServices>();
            _emailServicesMock = new Mock<IEmailServices>();
            _loggerMock = new Mock<ILogger<PaymentService>>();

            _paymentService = new PaymentService(
                _savingsAccountRepositoryMock.Object,
                _creditCardRepositoryMock.Object,
                _loansRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _cardConsumptionRepositoryMock.Object,
                _cardPaymentRepositoryMock.Object,
                _loanInstallmentRepositoryMock.Object,
                _loansPaymentRepositoryMock.Object,
                _validationServicesMock.Object,
                _emailServicesMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task PayCreditCardAsync_WithOverpayment_ShouldApplyAntiOverpaymentRuleAndDecreaseOnlyOwedAmount()
        {
            var dto = new PayCreditCardDto
            {
                SourceAccountNumber = "100000001",
                CreditCardId = 1,
                Amount = 1000m // Owed amount is only 400
            };
            var clientId = "client-123";

            var originAccount = new SavingsAccount
            {
                Id = 1,
                AccountNumber = "100000001",
                CustomerId = clientId,
                Balance = 1500m,
                AccountType = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Activa,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var creditCard = new CreditCard
            {
                Id = 1,
                CardNumber = "4000123456789010",
                LastFourDigits = "9010",
                CustomerId = clientId,
                CreditLimit = 2000m,
                OwedAmount = 400m, // Only owes 400
                Status = CreditCardStatus.Activa,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(2),
                CvcHash = "hash",
                AssignedByAdminId = "admin-123",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var effectiveAmount = 400m; // limit payment to owed amount

            _validationServicesMock.Setup(v => v.ValidateCreditCardPaymentAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, CreditCard, decimal)>.Success((originAccount, creditCard, effectiveAmount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _paymentService.PayCreditCardAsync(dto, clientId);

            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.EffectiveAmount.Should().Be(400m); // Anti-overpayment applied

            originAccount.Balance.Should().Be(1100m); // 1500 - 400
            creditCard.OwedAmount.Should().Be(0m); // fully paid

            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(originAccount), Times.Once);
            _creditCardRepositoryMock.Verify(r => r.UpdateAsync(creditCard), Times.Once);
            _cardPaymentRepositoryMock.Verify(r => r.AddAsync(It.Is<CardPayment>(p =>
                p.CreditCardId == creditCard.Id &&
                p.RequestedAmount == 1000m &&
                p.EffectiveAmount == 400m
            )), Times.Once);
        }
    }
}
