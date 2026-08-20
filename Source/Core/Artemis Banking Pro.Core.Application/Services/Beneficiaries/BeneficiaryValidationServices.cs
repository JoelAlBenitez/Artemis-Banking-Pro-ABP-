using Artemis_Banking_Pro.Core.Application.Contracts.Beneficiaries;
using Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Beneficiaries
{
    public sealed class BeneficiaryValidationServices : IBeneficiaryValidationServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly ILogger<BeneficiaryValidationServices> _logger;
        private readonly IUserManagementService _userManagementService;

        public BeneficiaryValidationServices(
            ISavingsAccountsRepository savingsAccountsRepository,
            IBeneficiaryRepository beneficiaryRepository,
            ILogger<BeneficiaryValidationServices> logger,
            IUserManagementService userManagementService)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
            _beneficiaryRepository = beneficiaryRepository;
            _logger = logger;
            _userManagementService = userManagementService;
        }

        public async Task<ValidationResult<SavingsAccount>> ValidateCreationAsync(SaveBeneficiaryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccountNumber) || dto.AccountNumber.Length != 9 || !dto.AccountNumber.All(char.IsDigit))
            {
                _logger.LogWarning("Validación fallida: el número de cuenta '{AccountNumber}' no tiene el formato válido de 9 dígitos", dto.AccountNumber);
                return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
            }

            try
            {
                var ownerUser = await _userManagementService.ValidateUserExistsByIdAsync(dto.OwnerClientId);
                if (!ownerUser.Exists || !ownerUser.IsActive)
                {
                    _logger.LogWarning("Validación fallida: el cliente propietario {ClientId} no existe o no está activo", dto.OwnerClientId);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
                }

                var savingsAccount = await _savingsAccountsRepository.GetFirstAsync(a => a.AccountNumber == dto.AccountNumber);
                if (savingsAccount is null)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de ahorros {AccountNumber} no existe", dto.AccountNumber);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
                }

                var beneficiaryUser = await _userManagementService.ValidateUserExistsByIdAsync(savingsAccount.CustomerId);
                if (!beneficiaryUser.Exists || !beneficiaryUser.IsActive)
                {
                    _logger.LogWarning("Validación fallida: el cliente beneficiario propietario de la cuenta {AccountNumber} no existe o no está activo", dto.AccountNumber);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountNotFound);
                }

                if (savingsAccount.Status != SavingsAccountStatus.Activa)
                {
                    _logger.LogWarning("Validación fallida: la cuenta de ahorros {AccountNumber} se encuentra cancelada o inactiva", dto.AccountNumber);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AccountCanceled);
                }

                if (savingsAccount.CustomerId == dto.OwnerClientId)
                {
                    _logger.LogWarning("Validación fallida: el cliente {ClientId} intentó agregarse a sí mismo como beneficiario", dto.OwnerClientId);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.OwnAccount);
                }

                var alreadyExists = await _beneficiaryRepository.ExistElementByConsult(b => 
                    b.OwnerClientId == dto.OwnerClientId && 
                    b.BeneficiarySavingsAccountId == savingsAccount.Id && 
                    b.IsActive);

                if (alreadyExists)
                {
                    _logger.LogWarning("Validación fallida: la cuenta {AccountNumber} ya se encuentra registrada como beneficiario activo para el cliente {ClientId}", dto.AccountNumber, dto.OwnerClientId);
                    return ValidationResult<SavingsAccount>.Failure(BeneficiaryError.AlreadyRegistered);
                }

                return ValidationResult<SavingsAccount>.Success(savingsAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al validar creación de beneficiario para el cliente {ClientId}", dto.OwnerClientId);
                return ValidationResult<SavingsAccount>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<Beneficiary>> ValidateDeactivationAsync(int id, string ownerClientId)
        {
            _logger.LogInformation("Iniciando validación de baja lógica de beneficiario ID {BeneficiaryId} para el cliente {ClientId}", id, ownerClientId);

            try
            {
                var beneficiary = await _beneficiaryRepository.GetFirstAsync(b => b.Id == id && b.OwnerClientId == ownerClientId);
                if (beneficiary is null)
                {
                    _logger.LogWarning("Validación fallida: el beneficiario ID {BeneficiaryId} no existe o no pertenece al cliente {ClientId}", id, ownerClientId);
                    return ValidationResult<Beneficiary>.Failure(BeneficiaryError.BeneficiaryNotFound);
                }

                return ValidationResult<Beneficiary>.Success(beneficiary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al validar la desactivación del beneficiario ID {BeneficiaryId} para el cliente {ClientId}", id, ownerClientId);
                return ValidationResult<Beneficiary>.Failure(GeneralError.UnexpectedError);
            }
        }
    }
}
