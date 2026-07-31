using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.CreditCards
{
    public interface ICreditCardRepository : IGenericRepository<CreditCard, int>
    {
    }
}
