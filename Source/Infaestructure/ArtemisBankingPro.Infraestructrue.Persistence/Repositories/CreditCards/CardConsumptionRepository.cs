using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards
{
    public sealed class CardConsumptionRepository :
        GenericRepository<CardConsumption, int>,
        ICardConsumptionRepository
    {
        public CardConsumptionRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
