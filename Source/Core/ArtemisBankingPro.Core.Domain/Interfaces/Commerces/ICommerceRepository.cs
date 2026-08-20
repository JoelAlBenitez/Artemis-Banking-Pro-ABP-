using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Commerces
{
    //Solo se declara lo que el repositorio genérico no resuelve con lambdas. La unicidad de
    //RNC y correo se comprueba con ExistElementByConsult.
    public interface ICommerceRepository : IGenericRepository<Commerce, int>
    {
        //Orden propio del módulo: del más reciente al más antiguo, filtrable por estado
        Task<PagedResult<Commerce>> GetPagedCommercesAsync(
            int page,
            int pageSize,
            CommerceStatus? status);
    }
}
