using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    //herencia se debe crear en la implementacion
    public interface ICreditCardsServices : 
       IGenericServices<CreditCardAssignmentDto, CreditCardDto, int>
     
    {
        Task<ValidationResult<PagedResult<CreditCardDto>>> GetPagedCreditCardsAsync(
            CreditCardFilterDto filter, string? customerId);

        Task<ValidationResult<PagedResult<CardConsumptionDto>>> GetPagedConsumptionsAsync(
            int creditCardId, int page);

        Task<ValidationResult<EditCardLimitDto>> GetCreditCardForEditLimitAsync(int creditCardId);

        Task<ValidationResult> AssignCreditCardAsync(CreditCardAssignmentDto dto);

        Task<ValidationResult<CardLimitUpdatedDto>> EditCreditCardLimitAsync(
            EditCardLimitDto dto);

        Task<ValidationResult> CancelCreditCardAsync(int creditCardId);

       
    }
}
