using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Transactions
{
    public sealed class CashAdvanceRepository :
        GenericRepository<CashAdvance, int>,
        ICashAdvanceRepository
    {
        public CashAdvanceRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
