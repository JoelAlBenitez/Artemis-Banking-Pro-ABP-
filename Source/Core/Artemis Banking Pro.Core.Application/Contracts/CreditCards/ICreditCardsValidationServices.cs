using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;

namespace Artemis_Banking_Pro.Core.Application.Contracts.CreditCards
{
    public interface ICreditCardsValidationServices
    {
        Task<ValidationResult> ValidateAssignmentAsync(CreditCardAssignmentDto dto);

        Task<ValidationResult<CreditCard>> ValidateActiveCreditCardAsync(int creditCardId);

        Task<ValidationResult<CreditCard>> ValidateLimitEditionAsync(EditCardLimitDto dto);

        Task<ValidationResult<CreditCard>> ValidateCancellationAsync(int creditCardId);
    }
}
