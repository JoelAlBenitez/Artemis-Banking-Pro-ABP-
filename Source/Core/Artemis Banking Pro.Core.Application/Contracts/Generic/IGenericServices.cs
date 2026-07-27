using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Generic
{
    public interface IGenericServices<TDto, Tkey>
        where TDto : class
    {
        Task<ValidationResult> CreateAsync(TDto dto);
        Task<ValidationResult> UpdateAsync(TDto dto);
        Task<ValidationResult<TDto>> GetByIdAsync(Tkey tkey);
        Task<ValidationResult<IReadOnlyCollection<TDto>>> GetAllAsync(int page, int pageSize);
    }
}
