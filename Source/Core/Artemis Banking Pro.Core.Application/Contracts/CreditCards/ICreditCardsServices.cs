using Artemis_Banking_Pro.Core.Application.Contracts.Generic;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    //herencia se debe crear en la implementacion
    public interface ICreditCardsServices  
     
    {
        Task<ValidationResult<PagedResult<CreditCardDto>>> GetPagedCreditCardsAsync(
            CreditCardFilterDto filter, string? customerId);

        Task<ValidationResult<PagedResult<CardConsumptionDto>>> GetPagedConsumptionsAsync(
            int creditCardId, int page);

        Task<ValidationResult<EditCardLimitDto>> GetCreditCardForEditLimitAsync(int creditCardId);

        Task<ValidationResult<CreditCardAssignedDto>> AssignCreditCardAsync(
            CreditCardAssignmentDto dto, string adminUserId);

        Task<ValidationResult<CardLimitUpdatedDto>> EditCreditCardLimitAsync(
            EditCardLimitDto dto, string adminUserId);

        Task<ValidationResult> CancelCreditCardAsync(int creditCardId, string adminUserId);

        Task<ValidationResult> SendCreditCardAssignedNotificationAsync(
            CreditCardAssignedDto assigned, string customerEmail, string customerFullName);

        Task<ValidationResult> SendCardLimitUpdatedNotificationAsync(
            CardLimitUpdatedDto limitUpdated, string customerEmail, string customerFullName);
    }
}
