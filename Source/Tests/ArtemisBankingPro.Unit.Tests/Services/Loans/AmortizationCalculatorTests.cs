using System;
using System.Linq;
using Artemis_Banking_Pro.Core.Application.Services.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArtemisBankingPro.Unit.Tests.Services.Loans
{
    public sealed class AmortizationCalculatorTests
    {
        private readonly AmortizationCalculator _calculator;

        public AmortizationCalculatorTests()
        {
            _calculator = new AmortizationCalculator(NullLogger<AmortizationCalculator>.Instance);
        }

        [Fact]
        public void CalculateMonthlyInstallment_WithZeroRate_ShouldDivideCapitalByInstallments()
        {
            var installment = _calculator.CalculateMonthlyInstallment(120_000m, 0m, 12);

            installment.Should().Be(10_000m);
        }

        [Fact]
        public void CalculateMonthlyInstallment_WithAnnualRate_ShouldFollowFrenchFormula()
        {
            var installment = _calculator.CalculateMonthlyInstallment(100_000m, 12m, 12);

            installment.Should().BeApproximately(8_884.88m, 0.05m);
        }

        [Fact]
        public void CalculateMonthlyInstallment_WithoutInstallments_ShouldReturnZero()
        {
            _calculator.CalculateMonthlyInstallment(100_000m, 12m, 0).Should().Be(0m);
        }

        [Fact]
        public void GenerateAmortizationTable_ShouldCreateOneInstallmentPerMonthOfTheTerm()
        {
            var createdAt = new DateTimeOffset(2026, 7, 5, 10, 30, 0, TimeSpan.Zero);

            var table = _calculator.GenerateAmortizationTable(100_000m, 12m, 12, createdAt, 1);

            table.Should().HaveCount(12);
            table.Select(i => i.InstallmentNumber).Should().BeInAscendingOrder();
            table.Should().OnlyContain(i => i.LoanId == 1);
        }

        [Fact]
        public void GenerateAmortizationTable_FirstInstallment_ShouldExpireTheSameDayOfTheFollowingMonth()
        {
            var createdAt = new DateTimeOffset(2026, 7, 5, 10, 30, 0, TimeSpan.Zero);

            var table = _calculator.GenerateAmortizationTable(100_000m, 12m, 3, createdAt, 1);

            table[0].DueDate.Should().Be(createdAt.AddMonths(1));
            table[1].DueDate.Should().Be(createdAt.AddMonths(2));
            table[2].DueDate.Should().Be(createdAt.AddMonths(3));
        }

        [Fact]
        public void GenerateAmortizationTable_ShouldLeaveEveryInstallmentPendingAndWithoutOverdueMark()
        {
            var table = _calculator.GenerateAmortizationTable(100_000m, 12m, 12, DateTimeOffset.UtcNow, 1);

            table.Should().OnlyContain(i => i.paymentStatus == PaymentStatus.Pendiente);
            table.Should().OnlyContain(i => !i.IsOverdue);
            table.Should().OnlyContain(i => i.PendingBalance == i.InstallmentValue);
        }

        [Fact]
        public void GenerateAmortizationTable_ShouldAmortizeExactlyTheApprovedCapital()
        {
            var table = _calculator.GenerateAmortizationTable(100_000m, 12m, 12, DateTimeOffset.UtcNow, 1);

            table.Sum(i => i.CapitalAmount).Should().Be(100_000m);
        }

        [Fact]
        public void RecalculateFutureInstallments_ShouldOnlyTouchPendingInstallmentsWithFutureDueDate()
        {
            var today = new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);
            var installments = new[]
            {
                BuildInstallment(1, today.AddMonths(-2), PaymentStatus.Pagada),
                BuildInstallment(2, today.AddMonths(-1), PaymentStatus.ParcialmentePagada),
                BuildInstallment(3, today.AddDays(-1), PaymentStatus.Pendiente),
                BuildInstallment(4, today.AddMonths(1), PaymentStatus.Pendiente),
                BuildInstallment(5, today.AddMonths(2), PaymentStatus.Pendiente)
            };

            var recalculated = _calculator.RecalculateFutureInstallments(installments, 20_000m, 24m, today);

            recalculated.Select(i => i.InstallmentNumber).Should().Equal(4, 5);
            installments[0].InstallmentValue.Should().Be(1_000m);
            installments[1].InstallmentValue.Should().Be(1_000m);
            installments[2].InstallmentValue.Should().Be(1_000m);
        }

        [Fact]
        public void RecalculateFutureInstallments_ShouldRedistributeOnlyThePendingCapital()
        {
            var today = new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);
            var installments = new[]
            {
                BuildInstallment(1, today.AddMonths(-1), PaymentStatus.Pagada),
                BuildInstallment(2, today.AddMonths(1), PaymentStatus.Pendiente),
                BuildInstallment(3, today.AddMonths(2), PaymentStatus.Pendiente)
            };

            var recalculated = _calculator.RecalculateFutureInstallments(installments, 20_000m, 0m, today);

            recalculated.Sum(i => i.CapitalAmount).Should().Be(20_000m);
            recalculated.Should().OnlyContain(i => i.PendingBalance == i.InstallmentValue);
        }

        [Fact]
        public void RecalculateFutureInstallments_WithoutFutureInstallments_ShouldReturnEmpty()
        {
            var today = new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);
            var installments = new[] { BuildInstallment(1, today.AddMonths(-1), PaymentStatus.Pagada) };

            _calculator.RecalculateFutureInstallments(installments, 0m, 12m, today).Should().BeEmpty();
        }

        private static LoanInstallment BuildInstallment(int number, DateTimeOffset dueDate, PaymentStatus status)
            => new()
            {
                LoanId = 1,
                InstallmentNumber = number,
                DueDate = dueDate,
                InstallmentValue = 1_000m,
                InterestAmount = 100m,
                CapitalAmount = 900m,
                PendingBalance = status == PaymentStatus.Pagada ? 0m : 1_000m,
                paymentStatus = status,
                CreatedAt = dueDate.AddMonths(-1),
                CreateByUserId = string.Empty
            };
    }
}
