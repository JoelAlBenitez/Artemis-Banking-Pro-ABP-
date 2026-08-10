using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
namespace Artemis_Banking_Pro.Core.Application.Contracts.Generic
{
    
    public interface IGenericServices<TSaveDto, TDto, Tkey>
        where TSaveDto : class
        where TDto : class
    {
        Task<ValidationResult> CreateAsync(TSaveDto dto);
        Task<ValidationResult> UpdateAsync(Tkey tkey, TSaveDto dto);
        Task<ValidationResult<TDto>> GetByIdAsync(Tkey tkey);
        Task<ValidationResult<PagedResult<TDto>>> GetAllAsync(int page, int pageSize);
    }
}
