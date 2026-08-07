using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Beneficiaries
{
    public sealed class BeneficiaryRepository :
        GenericRepository<Beneficiary, int>,
        IBeneficiaryRepository
    {
        public BeneficiaryRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
