using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;

namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    public interface ICreditCardsValidationServices
    {
        ValidationResult<string> ValidateAdministratorInSession();

        Task<ValidationResult> ValidateCustomerSelectionAsync(string customerId);

        Task<ValidationResult> ValidateAssignmentAsync(CreditCardAssignmentDto dto);

        Task<ValidationResult<string?>> ValidateCustomerCardsQueryAsync(CreditCardFilterDto filter);

        Task<ValidationResult<CreditCard>> ValidateActiveCreditCardAsync(int creditCardId);

        Task<ValidationResult<CreditCard>> ValidateLimitEditionAsync(EditCardLimitDto dto);

        Task<ValidationResult<CreditCard>> ValidateCancellationAsync(int creditCardId);
    }
}
