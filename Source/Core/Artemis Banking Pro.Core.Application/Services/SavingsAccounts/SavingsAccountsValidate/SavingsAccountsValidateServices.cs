using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.Contracts.Users.Session;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Errors;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts.SavingsAccountsValidate
{
  
    public sealed class SavingsAccountsValidateServices : ISavingsAccountsValidateServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SavingsAccountsValidateServices> _logger;

        public SavingsAccountsValidateServices(
            ISavingsAccountsRepository savingsAccountsRepository,
            IUserManagementService userManagementService,
            ICurrentUserService currentUserService,
            ILogger<SavingsAccountsValidateServices> logger)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
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
                _logger.LogWarning("Operación de cuentas de ahorro sin un administrador autenticado que atribuir");
                return ValidationResult<string>.Failure(SavingsAccountError.AdminUserRequired);
            }

            return ValidationResult<string>.Success(adminUserId);
        }

        public async Task<ValidationResult> ValidateCustomerSelectionAsync(string customerId)
        {
            _logger.LogInformation("Validando el cliente {CustomerId} seleccionado para asignarle una cuenta de ahorro secundaria",
                customerId);

            if (string.IsNullOrWhiteSpace(customerId))
            {
                _logger.LogWarning("Intento de asignación de cuenta de ahorro sin cliente seleccionado");
                return ValidationResult.Failure(SavingsAccountError.CustomerRequired);
            }

            try
            {
                //La existencia del cliente y su estado activo los resuelve Identity
                var customer = await _userManagementService.ValidateUserExistsByIdAsync(customerId);

                if (!customer.Exists)
                {
                    _logger.LogWarning("El cliente {CustomerId} no existe", customerId);
                    return ValidationResult.Failure(SavingsAccountError.NonExistsCustomerByIdCard);
                }

                if (!customer.IsActive)
                {
                    _logger.LogWarning("El cliente {CustomerId} no se encuentra activo", customerId);
                    return ValidationResult.Failure(SavingsAccountError.CustomerIsNotActive);
                }

                var hasActivePrimaryAccount = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.CustomerId == customerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (!hasActivePrimaryAccount)
                {
                    _logger.LogWarning("El cliente {CustomerId} no posee una cuenta de ahorro principal activa",
                        customerId);

                    return ValidationResult.Failure(SavingsAccountError.CustomerWithoutActivePrimaryAccount);
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el cliente {CustomerId} para la asignación de una cuenta de ahorro",
                    customerId);

                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult> ValidateAssignmentAsync(SavingsAccountAssignmentDto dto)
        {
            if (dto is null)
            {
                _logger.LogWarning("Datos de asignación de cuenta de ahorro inválidos");
                return ValidationResult.Failure(GeneralError.DataInvalid);
            }

            _logger.LogInformation("Validando la asignación de una cuenta de ahorro secundaria al cliente {CustomerId}",
                dto.CustomerId);

            var customerValidation = await ValidateCustomerSelectionAsync(dto.CustomerId);
            if (!customerValidation.IsValid)
            {
                return customerValidation;
            }

            var errors = new List<Error>();

            if (dto.InitialBalance < 0m)
            {
                errors.Add(SavingsAccountError.NegativeInitialBalance);
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning("Asignación de cuenta de ahorro rechazada para el cliente {CustomerId}. Reglas incumplidas: {Reglas}",
                    dto.CustomerId, errors.Select(e => e.Description));

                return ValidationResult.Failure(errors);
            }

            return ValidationResult.Success();
        }

        public async Task<ValidationResult<SavingsAccount>> ValidateActiveSavingsAccountAsync(int savingsAccountId)
        {
            try
            {
                _logger.LogInformation("Validando la existencia y el estado de la cuenta de ahorro con ID {SavingsAccountId}",
                    savingsAccountId);

                var savingsAccount = await _savingsAccountsRepository.GetByIdAsync(savingsAccountId);

                if (savingsAccount is null)
                {
                    _logger.LogWarning("Cuenta de ahorro con ID {SavingsAccountId} inexistente", savingsAccountId);
                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.NonExistsSavingsAccount);
                }

                if (savingsAccount.Status != SavingsAccountStatus.Activa)
                {
                    _logger.LogWarning("La cuenta de ahorro {AccountNumber} ya se encuentra cancelada",
                        savingsAccount.AccountNumber);

                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.SavingsAccountAlreadyCancelled);
                }

                return ValidationResult<SavingsAccount>.Success(savingsAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la cuenta de ahorro con ID {SavingsAccountId}", savingsAccountId);
                return ValidationResult<SavingsAccount>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<SavingsAccount>> ValidateCancellationAsync(int savingsAccountId)
        {
            _logger.LogInformation("Validando la cancelación de la cuenta de ahorro con ID {SavingsAccountId}",
                savingsAccountId);

            var accountValidation = await ValidateActiveSavingsAccountAsync(savingsAccountId);
            if (!accountValidation.IsValid)
            {
                return accountValidation;
            }

            var savingsAccount = accountValidation.Value!;

            //Las principales no muestran la acción Cancelar y tampoco pueden cancelarse por URL
            if (savingsAccount.IsPrimary)
            {
                _logger.LogWarning("Cancelación rechazada: la cuenta {AccountNumber} es principal",
                    savingsAccount.AccountNumber);

                return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.PrimaryAccountCannotBeCancelled);
            }

            try
            {
                var hasActivePrimaryAccount = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.CustomerId == savingsAccount.CustomerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (!hasActivePrimaryAccount)
                {
                    _logger.LogWarning("Cancelación rechazada: el cliente {CustomerId} no tiene una cuenta principal activa receptora",
                        savingsAccount.CustomerId);

                    return ValidationResult<SavingsAccount>.Failure(SavingsAccountError.WithoutPrimaryAccountToReceiveFunds);
                }

                return ValidationResult<SavingsAccount>.Success(savingsAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la cancelación de la cuenta de ahorro con ID {SavingsAccountId}",
                    savingsAccountId);

                return ValidationResult<SavingsAccount>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<string?>> ValidateCustomerAccountsQueryAsync(SavingsAccountFilterDto filter)
        {
            if (filter is null)
            {
                _logger.LogWarning("Filtros de consulta de cuentas de ahorro inválidos");
                return ValidationResult<string?>.Failure(GeneralError.DataInvalid);
            }

            if (string.IsNullOrWhiteSpace(filter.IdCard))
            {
                return ValidationResult<string?>.Success(null);
            }

            try
            {
                //La cédula identifica al cliente dentro de Identity: aquí se traduce a su Id,
                //que es la única clave con la que este módulo relaciona las cuentas.
                var customer = await _userManagementService.GetClientByIdCardAsync(filter.IdCard);

                if (customer is null)
                {
                    _logger.LogWarning("No existe un cliente registrado con la cédula {IdCard}", filter.IdCard);
                    return ValidationResult<string?>.Failure(SavingsAccountError.NonExistsCustomerByIdCard);
                }

                _logger.LogInformation("Consulta de cuentas de ahorro por la cédula {IdCard}", filter.IdCard);

                return ValidationResult<string?>.Success(customer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver el cliente de la cédula {IdCard}", filter.IdCard);
                return ValidationResult<string?>.Failure(GeneralError.UnexpectedError);
            }
        }
    }
}
