using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.Services.Transactions;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Transactions
{
    public class LoanPaymentServiceTests
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

        public LoanPaymentServiceTests()
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
        public async Task PayLoanAsync_WithOverpayment_ShouldApplyAntiOverpaymentRuleAndDecreaseOnlyOwedAmount()
        {
            // Arrange
            var dto = new PayLoanDto
            {
                SourceAccountNumber = "100000001",
                LoanId = 1,
                Amount = 1000m // Intentando pagar 1000
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

            var loan = new Loan
            {
                Id = 1,
                LoanNumber = "LN00001",
                CustomerId = clientId,
                ApprovedCapital = 500m,
                PendingAmount = 400m, // Solo debe 400
                Status = LoanStatus.Activo,
                AnnualInterestRate = 12,
                termMonths = TermMonths.Meses12,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var installments = new List<LoanInstallment>
            {
                new LoanInstallment 
                { 
                    Id = 1, 
                    LoanId = 1, 
                    InstallmentNumber = 1,
                    DueDate = DateTimeOffset.UtcNow.AddMonths(1),
                    InstallmentValue = 400m,
                    InterestAmount = 0m,
                    CapitalAmount = 400m,
                    PendingBalance = 400m,
                    paymentStatus = PaymentStatus.Pendiente,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                }
            };

            var effectiveAmount = 400m; // limit payment to pending amount

            _validationServicesMock.Setup(v => v.ValidateLoanPaymentAsync(dto, clientId))
                .ReturnsAsync(ValidationResult<(SavingsAccount, Loan, List<LoanInstallment>, decimal)>.Success((originAccount, loan, installments, effectiveAmount)));

            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            _transactionRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            _loanInstallmentRepositoryMock.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _paymentService.PayLoanAsync(dto, clientId);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.EffectiveAmount.Should().Be(400m); // Anti-overpayment applied

            originAccount.Balance.Should().Be(1100m); // 1500 - 400
            loan.PendingAmount.Should().Be(0m); // fully paid
            loan.Status.Should().Be(LoanStatus.Completado); // Marked as completed

            _savingsAccountRepositoryMock.Verify(r => r.UpdateAsync(originAccount), Times.Once);
            _loansRepositoryMock.Verify(r => r.UpdateAsync(loan), Times.Once);
            _loanInstallmentRepositoryMock.Verify(r => r.UpdateAsync(It.Is<LoanInstallment>(i => i.Id == 1 && i.PendingBalance == 0m && i.paymentStatus == PaymentStatus.Pagada)), Times.Once);
            _loansPaymentRepositoryMock.Verify(r => r.AddAsync(It.Is<LoanPayment>(p =>
                p.LoandId == loan.Id &&
                p.RequestedAmount == 1000m &&
                p.EffectiveAmount == 400m
            )), Times.Once);
        }
    }
}
