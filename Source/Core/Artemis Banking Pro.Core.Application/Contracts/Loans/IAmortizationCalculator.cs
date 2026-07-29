using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface IAmortizationCalculator
    {
        //Cuota fija por sistema francés. Con tasa 0% es capital entre cuotas.
        decimal CalculateMonthlyInstallment(decimal capital, decimal annualInterestRate, int totalInstallments);

        //Tabla completa: la primera cuota vence el mismo día del mes siguiente
        //a la creación del préstamo.
        IReadOnlyList<LoanInstallment> GenerateAmortizationTable(
            decimal capital,
            decimal annualInterestRate,
            int totalInstallments,
            DateTimeOffset loanCreatedAt,
            string createByUserId);

        //Recálculo por cambio de tasa: solo cuotas pendientes con vencimiento
        //posterior a hoy. Devuelve las cuotas modificadas.
        IReadOnlyList<LoanInstallment> RecalculateFutureInstallments(
            IReadOnlyCollection<LoanInstallment> installments,
            decimal pendingCapital,
            decimal newAnnualInterestRate,
            DateTimeOffset today,
            string modifiedByUserId);
    }
}
