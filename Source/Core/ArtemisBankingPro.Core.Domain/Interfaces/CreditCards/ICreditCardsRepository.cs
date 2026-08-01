using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.CreditCards
{
    public interface ICreditCardsRepository :
        IGenericRepository<CreditCard, int>
    {
        Task<PagedResult<CreditCard>> GetPagedCreditCardsAsync(
            int page,
            int pageSize,
            CreditCardStatus? status,
            string? customerId);

        Task<CreditCard?> GetCreditCardWithConsumptionsAsync(int creditCardId);
    }
}
