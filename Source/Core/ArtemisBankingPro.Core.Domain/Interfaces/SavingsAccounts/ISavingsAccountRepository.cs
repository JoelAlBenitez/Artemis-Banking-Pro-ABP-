using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts
{
    public interface ISavingsAccountRepository : IGenericRepository<SavingsAccount, int>
    {
    }
}
