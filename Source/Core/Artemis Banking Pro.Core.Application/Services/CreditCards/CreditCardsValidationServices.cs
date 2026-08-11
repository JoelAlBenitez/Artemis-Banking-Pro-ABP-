using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
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
        private readonly IUserManagementService _userManagementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreditCardsValidationServices> _logger;

        public CreditCardsValidationServices(
            ICreditCardsRepository creditCardsRepository,
            IUserManagementService userManagementService,
            ICurrentUserService currentUserService,
            ILogger<CreditCardsValidationServices> logger)
        {
            _creditCardsRepository = creditCardsRepository;
            _userManagementService = userManagementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public ValidationResult<string> ValidateAdministratorInSession()
        {
            var adminUserId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(adminUserId)
                || !_currentUserService.IsInRole(Roles.Administrador.ToString()))
            {
                _logger.LogWarning("Operación de tarjetas de crédito sin un administrador autenticado que atribuir");
                return ValidationResult<string>.Failure(CreditCardError.AdminUserRequired);
            }

            return ValidationResult<string>.Success(adminUserId);
        }

        public async Task<ValidationResult> ValidateCustomerSelectionAsync(string customerId)
        {
            _logger.LogInformation("Validando el cliente {CustomerId} seleccionado para asignarle una tarjeta de crédito",
                customerId);

            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignación de tarjeta de crédito sin cliente seleccionado");
                return ValidationResult.Failure(CreditCardError.CustomerRequired);
            }

            try
            {
                var customer = await _userManagementService.ValidateUserExistsByIdAsync(customerId);

                if (!customer.Exists)
                {
                    _logger.LogWarning("El cliente {CustomerId} no existe", customerId);
                    return ValidationResult.Failure(CreditCardError.NonExistsCustomerByIdCard);
                }

                if (!customer.IsActive)
                {
                    _logger.LogWarning("El cliente {CustomerId} no se encuentra activo", customerId);
                    return ValidationResult.Failure(CreditCardError.CustomerIsNotActive);
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el cliente {CustomerId} para la asignación de una tarjeta de crédito",
                    customerId);

                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult> ValidateAssignmentAsync(CreditCardAssignmentDto dto)
        {
            if (dto is null)
            {
                _logger.LogWarning("Datos de asignación de tarjeta de crédito inválidos");
                return ValidationResult.Failure(GeneralError.DataInvalid);
            }

            _logger.LogInformation("Validando la asignación de una tarjeta de crédito al cliente {CustomerId}",
                dto.CustomerId);

            var customerValidation = await ValidateCustomerSelectionAsync(dto.CustomerId);
            if (!customerValidation.IsValid)
            {
                return customerValidation;
            }

            var errors = new List<Error>();

            if (dto.CreditLimit <= 0m)
            {
                errors.Add(CreditCardError.InvalidCreditLimitAssignment);
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Asignación de tarjeta rechazada para el cliente {CustomerId}. Reglas incumplidas: {Reglas}",
                    dto.CustomerId, errors.Select(e => e.Description));

                return ValidationResult.Failure(errors);
            }

            return ValidationResult.Success();
        }

        public async Task<ValidationResult<string?>> ValidateCustomerCardsQueryAsync(CreditCardFilterDto filter)
        {
            if (filter is null)
            {
                _logger.LogWarning("Filtros de consulta de tarjetas de crédito inválidos");
                return ValidationResult<string?>.Failure(GeneralError.DataInvalid);
            }

            if (string.IsNullOrWhiteSpace(filter.IdCard))
            {
                return ValidationResult<string?>.Success(null);
            }

            try
            {
                var customer = await _userManagementService.GetClientByIdCardAsync(filter.IdCard);

                if (customer is null)
                {
                    _logger.LogWarning("No existe un cliente registrado con la cédula {IdCard}", filter.IdCard);
                    return ValidationResult<string?>.Failure(CreditCardError.NonExistsCustomerByIdCard);
                }

                _logger.LogInformation("Consulta de tarjetas de crédito por la cédula {IdCard}", filter.IdCard);

                return ValidationResult<string?>.Success(customer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver el cliente de la cédula {IdCard}", filter.IdCard);
                return ValidationResult<string?>.Failure(GeneralError.UnexpectedError);
            }
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
            if (dto is null)
            {
                _logger.LogWarning("Datos de modificación de límite de tarjeta inválidos");
                return ValidationResult<CreditCard>.Failure(GeneralError.DataInvalid);
            }

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
