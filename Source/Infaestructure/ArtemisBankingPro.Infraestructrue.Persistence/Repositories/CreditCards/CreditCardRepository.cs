using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards
{
    public sealed class CreditCardRepository :
        GenericRepository<CreditCard, int>,
        ICreditCardRepository
    {
        public CreditCardRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
