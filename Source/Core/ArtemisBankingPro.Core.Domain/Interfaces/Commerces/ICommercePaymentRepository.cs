using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Commerces
{
    public interface ICommercePaymentRepository : IGenericRepository<CommercePayment, int>
    {
        //Transacciones recibidas por un comercio, de la más reciente a la más antigua
        Task<PagedResult<CommercePayment>> GetPagedPaymentsByCommerceAsync(
            int commerceId,
            int page,
            int pageSize);
    }
}
