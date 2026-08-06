using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Artemis_Banking_Pro.Core.Application.Services.Debts;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Loans
{
    public sealed class DebtCalculatorTests
    {
        private readonly Mock<ILoansRepository> _loansRepositoryMock;
        private readonly Mock<ICreditCardsRepository> _creditCardsRepositoryMock;
        private readonly DebtCalculator _debtCalculator;

        public DebtCalculatorTests()
        {
            _loansRepositoryMock = new Mock<ILoansRepository>();
            _creditCardsRepositoryMock = new Mock<ICreditCardsRepository>();

            _debtCalculator = new DebtCalculator(
                _loansRepositoryMock.Object,
                _creditCardsRepositoryMock.Object,
                NullLogger<DebtCalculator>.Instance);
        }

        [Fact]
        public async Task GetCustomerDebtAsync_ShouldAddActiveLoansAndActiveCards()
        {
            _loansRepositoryMock
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, decimal>>>()))
                .ReturnsAsync(76_250m);

            _creditCardsRepositoryMock
                .Setup(repository => repository.SumAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, decimal>>>()))
                .ReturnsAsync(12_000m);

            var debt = await _debtCalculator.GetCustomerDebtAsync("customer-1");

            debt.Should().Be(88_250m);
        }

        [Fact]
        public async Task GetCustomerDebtAsync_WithoutCustomer_ShouldReturnZero()
        {
            (await _debtCalculator.GetCustomerDebtAsync(" ")).Should().Be(0m);
        }

        [Fact]
        public async Task GetAverageDebtAsync_ShouldDivideTheTotalDebtBetweenTheActiveCustomers()
        {
            GivenActiveLoans(
                BuildLoan("customer-1", 100_000m),
                BuildLoan("customer-2", 60_000m));

            GivenActiveCards(BuildCard("customer-1", 20_000m));

            var average = await _debtCalculator.GetAverageDebtAsync();

            average.Should().Be(90_000m);
        }

        [Fact]
        public async Task GetAverageDebtAsync_WithoutActiveCustomers_ShouldReturnZero()
        {
            GivenActiveLoans();
            GivenActiveCards();

            (await _debtCalculator.GetAverageDebtAsync()).Should().Be(0m);
        }

        [Fact]
        public async Task GetAverageDebtAsync_WithActiveCustomersWithoutProducts_ShouldIncludeThemInTheDivisor()
        {
            GivenActiveLoans(BuildLoan("customer-1", 100_000m));
            GivenActiveCards();

            var average = await _debtCalculator.GetAverageDebtAsync(new[] { "customer-1", "customer-2" });

            average.Should().Be(50_000m);
        }

        [Fact]
        public async Task GetCustomersDebtAsync_ShouldReturnTheDebtOfEveryRequestedCustomer()
        {
            GivenActiveLoans(BuildLoan("customer-1", 100_000m));
            GivenActiveCards(BuildCard("customer-2", 5_000m));

            var debts = await _debtCalculator.GetCustomersDebtAsync(new[] { "customer-1", "customer-2" });

            debts["customer-1"].Should().Be(100_000m);
            debts["customer-2"].Should().Be(5_000m);
        }

        [Fact]
        public void GetProjectedDebt_ShouldAddTheTotalPayableOfTheNewLoan()
        {
            _debtCalculator.GetProjectedDebt(25_000m, 107_500m).Should().Be(132_500m);
        }

        #region helpers
        private void GivenActiveLoans(params Loan[] loans)
            => _loansRepositoryMock
                .Setup(repository => repository.GetAllFindAsync(
                    It.IsAny<Expression<Func<Loan, bool>>>(),
                    It.IsAny<Expression<Func<Loan, object>>[]>()))
                .ReturnsAsync(loans);

        private void GivenActiveCards(params CreditCard[] cards)
            => _creditCardsRepositoryMock
                .Setup(repository => repository.GetAllFindAsync(
                    It.IsAny<Expression<Func<CreditCard, bool>>>(),
                    It.IsAny<Expression<Func<CreditCard, object>>[]>()))
                .ReturnsAsync(cards);

        private static Loan BuildLoan(string customerId, decimal pendingAmount)
            => new()
            {
                LoanNumber = "100000001",
                CustomerId = customerId,
                ApprovedCapital = pendingAmount,
                termMonths = TermMonths.Meses12,
                AnnualInterestRate = 12m,
                PendingAmount = pendingAmount,
                Status = LoanStatus.Activo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1",
                loanInstallments = new List<LoanInstallment>()
            };

        private static CreditCard BuildCard(string customerId, decimal owedAmount)
            => new()
            {
                CardNumber = "4000000000000001",
                LastFourDigits = "0001",
                CustomerId = customerId,
                CreditLimit = 100_000m,
                OwedAmount = owedAmount,
                ExpirationDate = DateTimeOffset.UtcNow.AddYears(3),
                CvcHash = new string('a', 64),
                Status = CreditCardStatus.Activa,
                AssignedByAdminId = "admin-1",
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = "admin-1"
            };
        #endregion
    }
}
