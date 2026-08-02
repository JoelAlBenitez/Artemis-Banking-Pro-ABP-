using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.CreditCards
{
    public sealed class CreditCardsValidationServices : ICreditCardsValidationServices
    {
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ILogger<CreditCardsValidationServices> _logger;

        public CreditCardsValidationServices(
            ICreditCardsRepository creditCardsRepository,
            ILogger<CreditCardsValidationServices> logger)
        {
            _creditCardsRepository = creditCardsRepository;
            _logger = logger;
        }

        public Task<ValidationResult> ValidateAssignmentAsync(CreditCardAssignmentDto dto)
        {
            _logger.LogInformation("Validando la asignación de una tarjeta de crédito al cliente {CustomerId}",
                dto.CustomerId);

            var errors = new List<Error>();

            if (string.IsNullOrWhiteSpace(dto.CustomerId))
            {
                errors.Add(CreditCardError.CustomerRequired);
            }

            if (dto.CreditLimit <= 0m)
            {
                errors.Add(CreditCardError.InvalidCreditLimitAssignment);
            }

            //La existencia del cliente y su estado activo se validan contra el project Identity.
            //Cuando el servicio de consulta de usuarios esté disponible, esta validación debe consultarlo
            //y agregar CreditCardError.NonExistsCustomerByIdCard o CreditCardError.CustomerIsNotActive.
            //var customer = await _userServices.GetCustomerByIdAsync(dto.CustomerId);
            //if (customer is null) errors.Add(CreditCardError.NonExistsCustomerByIdCard);
            //else if (!customer.IsActive) errors.Add(CreditCardError.CustomerIsNotActive);

            if (errors.Count > 0)
            {
                _logger.LogWarning("Asignación de tarjeta rechazada para el cliente {CustomerId}. Reglas incumplidas: {Reglas}",
                    dto.CustomerId, errors.Select(e => e.Description));

                return Task.FromResult(ValidationResult.Failure(errors));
            }

            return Task.FromResult(ValidationResult.Success());
        }

        public async Task<ValidationResult<CreditCard>> ValidateActiveCreditCardAsync(int creditCardId)
        {
            try
            {
                _logger.LogInformation("Validando la existencia y el estado de la tarjeta con ID {CreditCardId}",
                    creditCardId);

                var creditCard = await _creditCardsRepository.GetByIdAsync(creditCardId);

                if (creditCard is null)
                {
                    _logger.LogWarning("Tarjeta de crédito con ID {CreditCardId} inexistente", creditCardId);
                    return ValidationResult<CreditCard>.Failure(CreditCardError.NonExistsCreditCard);
                }

                if (creditCard.Status != CreditCardStatus.Activa)
                {
                    _logger.LogWarning("La tarjeta terminada en {LastFourDigits} se encuentra cancelada",
                        creditCard.LastFourDigits);

                    return ValidationResult<CreditCard>.Failure(CreditCardError.CreditCardIsCancelled);
                }

                return ValidationResult<CreditCard>.Success(creditCard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la tarjeta de crédito con ID {CreditCardId}", creditCardId);
                return ValidationResult<CreditCard>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<CreditCard>> ValidateLimitEditionAsync(EditCardLimitDto dto)
        {
            _logger.LogInformation("Validando la modificación del límite de la tarjeta con ID {CreditCardId}", dto.Id);

            var creditCardValidation = await ValidateActiveCreditCardAsync(dto.Id);
            if (!creditCardValidation.IsValid)
            {
                return creditCardValidation;
            }

            var creditCard = creditCardValidation.Value!;
            var errors = new List<Error>();

            if (dto.CreditLimit <= 0m)
            {
                errors.Add(CreditCardError.InvalidCreditLimit);
            }

            if (dto.CreditLimit < creditCard.OwedAmount)
            {
                errors.Add(CreditCardError.CreditLimitLowerThanOwedAmount);
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Modificación de límite rechazada para la tarjeta terminada en {LastFourDigits}. Reglas incumplidas: {Reglas}",
                    creditCard.LastFourDigits, errors.Select(e => e.Description));

                return ValidationResult<CreditCard>.Failure(errors);
            }

            return ValidationResult<CreditCard>.Success(creditCard);
        }

        public async Task<ValidationResult<CreditCard>> ValidateCancellationAsync(int creditCardId)
        {
            _logger.LogInformation("Validando la cancelación de la tarjeta con ID {CreditCardId}", creditCardId);

            var creditCardValidation = await ValidateActiveCreditCardAsync(creditCardId);
            if (!creditCardValidation.IsValid)
            {
                return creditCardValidation;
            }

            var creditCard = creditCardValidation.Value!;

            if (creditCard.OwedAmount > 0m)
            {
                _logger.LogWarning("Cancelación rechazada: la tarjeta terminada en {LastFourDigits} mantiene deuda pendiente",
                    creditCard.LastFourDigits);

                return ValidationResult<CreditCard>.Failure(CreditCardError.CreditCardWithPendingDebt);
            }

            return ValidationResult<CreditCard>.Success(creditCard);
        }
    }
}
