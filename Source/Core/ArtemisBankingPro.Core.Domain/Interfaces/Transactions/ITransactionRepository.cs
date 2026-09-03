using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using ArtemisBankingPro.Core.Domain.Common.Enum;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Transactions
{
    public interface ITransactionRepository : IGenericRepository<Transaction, int>
    {
        Task<int> GetTotalHistoricalAsync();
        Task<int> GetTotalTodayAsync();
        Task<IReadOnlyList<Transaction>> GetPaymentsAsync(ChannelPayment? channel, DateTimeOffset? date);
        Task<IReadOnlyList<Transaction>> GetTransactionsFromDateAsync(DateTimeOffset fromDate);
    }
}
