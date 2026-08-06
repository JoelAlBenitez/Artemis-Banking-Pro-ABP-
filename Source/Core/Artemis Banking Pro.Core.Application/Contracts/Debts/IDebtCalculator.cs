namespace Artemis_Banking_Pro.Core.Application.Contracts.Debts
{
    //Deuda de un cliente = monto pendiente de sus préstamos activos + monto adeudado en sus
    //tarjetas de crédito activas. Lo consumen la evaluación de riesgo de préstamos, el paso 1
    //de asignación de productos y el indicador de deuda promedio del dashboard.
    public interface IDebtCalculator
    {
        Task<decimal> GetCustomerDebtAsync(string customerId);

        Task<IReadOnlyDictionary<string, decimal>> GetCustomersDebtAsync(IReadOnlyCollection<string> customerIds);

        //Deuda total de los clientes activos entre la cantidad de clientes activos. Sin clientes
        //activos el promedio es RD$0.00.
        Task<decimal> GetAverageDebtAsync(IReadOnlyCollection<string>? activeCustomerIds = null);

        decimal GetProjectedDebt(decimal currentDebt, decimal totalPayableOfNewLoan);
    }
}
