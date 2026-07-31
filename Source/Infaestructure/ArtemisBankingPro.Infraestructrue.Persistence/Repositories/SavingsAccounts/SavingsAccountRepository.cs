using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.SavingsAccounts
{
    public sealed class SavingsAccountRepository :
        GenericRepository<SavingsAccount, int>,
        ISavingsAccountRepository
    {
        public SavingsAccountRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
