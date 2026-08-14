namespace Artemis_Banking_Pro.Core.Application.Contracts.Debts
{
   
    public interface IDebtCalculator
    {
        Task<decimal> GetCustomerDebtAsync(string customerId);

        Task<IReadOnlyDictionary<string, decimal>> GetCustomersDebtAsync(IReadOnlyCollection<string> customerIds);

        Task<decimal> GetAverageDebtAsync(IReadOnlyCollection<string>? activeCustomerIds = null);

        decimal GetProjectedDebt(decimal currentDebt, decimal totalPayableOfNewLoan);
    }
}
