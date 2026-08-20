using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface IAmortizationCalculator
    {
        decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int totalInstallments);
       
        //El administrador responsable llega por parámetro: el calculador no consulta la sesión,
        //así se mantiene puro y verificable con valores fijos.
        IReadOnlyList<LoanInstallment> GenerateAmortizationTable(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments,
            DateTimeOffset loanCreatedAt,
            int loanId,
            string createdByUserId);

        IReadOnlyList<LoanInstallment> RecalculateFutureInstallments(
            IReadOnlyCollection<LoanInstallment> installments,
            decimal pendingCapital,
            decimal newAnnualInterestRate,
            DateTimeOffset today,
            string modifiedByUserId);
    }
}
