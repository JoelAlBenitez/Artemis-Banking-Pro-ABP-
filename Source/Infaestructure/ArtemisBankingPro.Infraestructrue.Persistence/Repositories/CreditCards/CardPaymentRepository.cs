using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards
{
    public sealed class CardPaymentRepository :
        GenericRepository<CardPayment, int>,
        ICardPaymentRepository
    {
        public CardPaymentRepository(DbContextArtemisBanking context) : base(context) { }
    }
}
