using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries
{
    public interface IBeneficiaryRepository : IGenericRepository<Beneficiary, int>
    {
    }
}
