using Artemis_Banking_Pro.Core.Application.Contracts.Loans;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Services.Loans
{
    public sealed class AmortizationCalculator : IAmortizationCalculator
    {
        private const int MoneyDecimals = 2;

        public decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int totalInstallments)
        {
            if (totalInstallments <= 0) return 0m;
            var monthlyRate = MonthlyRate(annualInterestRate);
            if (monthlyRate == 0m) return Money(capital / totalInstallments);
            var compound = Compound(monthlyRate, totalInstallments);
            return Money(capital * (monthlyRate * compound) / (compound - 1m));
        }

        public IReadOnlyList<LoanInstallment> GenerateAmortizationTable(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments,
            DateTimeOffset loanCreatedAt,
            string createByUserId)
        {
            var installments = new List<LoanInstallment>();
            if (totalInstallments <= 0) return installments;

            var monthlyRate = MonthlyRate(annualInterestRate);
            var monthlyInstallment = CalculateMonthlyInstallment(capital, annualInterestRate, totalInstallments);
            var pendingCapital = capital;

            for (var number = 1; number <= totalInstallments; number++)
            {
                var interest = Money(pendingCapital * monthlyRate);
                var capitalAmount = Money(monthlyInstallment - interest);
                var installmentValue = monthlyInstallment;

                if (number == totalInstallments)
                {
                    capitalAmount = pendingCapital;
                    installmentValue = Money(capitalAmount + interest);
                }

                pendingCapital = Money(pendingCapital - capitalAmount);

                installments.Add(new LoanInstallment
                {
                    LoanId = 0,// cambiar para recibir el id del prestamo por el service llamado de manager loans 
                    //al momento de la asignacion.
                    InstallmentNumber = number,
                    DueDate = loanCreatedAt.AddMonths(number),
                    InstallmentValue = installmentValue,
                    InterestAmount = interest,
                    CapitalAmount = capitalAmount,
                    PendingBalance = installmentValue,
                    paymentStatus = PaymentStatus.Pendiente,
                    IsOverdue = false,
                    CreatedAt = loanCreatedAt,
                    CreateByUserId = createByUserId
                });
            }

            return installments;
        }

        public IReadOnlyList<LoanInstallment> RecalculateFutureInstallments(
            IReadOnlyCollection<LoanInstallment> installments,
            decimal pendingCapital,
            decimal newAnnualInterestRate,
            DateTimeOffset today,
            string modifiedByUserId)
        {
            var futureInstallments = installments
                .Where(i => i.paymentStatus == PaymentStatus.Pendiente && i.DueDate > today)
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            if (futureInstallments.Count == 0) return futureInstallments;

            var monthlyRate = MonthlyRate(newAnnualInterestRate);
            var monthlyInstallment = CalculateMonthlyInstallment(
                pendingCapital, newAnnualInterestRate, futureInstallments.Count);

            for (var index = 0; index < futureInstallments.Count; index++)
            {
                var installment = futureInstallments[index];

                var interest = Money(pendingCapital * monthlyRate);
                var capitalAmount = Money(monthlyInstallment - interest);
                var installmentValue = monthlyInstallment;

                if (index == futureInstallments.Count - 1)
                {
                    capitalAmount = pendingCapital;
                    installmentValue = Money(capitalAmount + interest);
                }

                pendingCapital = Money(pendingCapital - capitalAmount);

                installment.InstallmentValue = installmentValue;
                installment.InterestAmount = interest;
                installment.CapitalAmount = capitalAmount;
                installment.PendingBalance = installmentValue;
                installment.ModifiedAt = today;
                installment.LastModifiedByIdUser = modifiedByUserId;
            }

            return futureInstallments;
        }

        private static decimal MonthlyRate(decimal annualInterestRate)
            => annualInterestRate <= 0m ? 0m : annualInterestRate / 100m / 12m;

        private static decimal Compound(decimal monthlyRate, int totalInstallments)
        {
            var compound = 1m;
            for (var i = 0; i < totalInstallments; i++) compound *= 1m + monthlyRate;
            return compound;
        }

        private static decimal Money(decimal value)
            => Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
    }
}
