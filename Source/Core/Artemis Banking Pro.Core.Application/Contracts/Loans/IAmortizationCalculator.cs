using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface IAmortizationCalculator
    {
        decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int totalInstallments);
       
        IReadOnlyList<LoanInstallment> GenerateAmortizationTable(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments,
            DateTimeOffset loanCreatedAt,
            int loanId);

        IReadOnlyList<LoanInstallment> RecalculateFutureInstallments(
            IReadOnlyCollection<LoanInstallment> installments,
            decimal pendingCapital,
            decimal newAnnualInterestRate,
            DateTimeOffset today);
    }
}
