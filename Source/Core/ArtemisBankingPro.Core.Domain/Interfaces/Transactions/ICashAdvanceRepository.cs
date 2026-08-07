using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Transactions
{
    public interface ICashAdvanceRepository : IGenericRepository<CashAdvance, int>
    {
    }
}
