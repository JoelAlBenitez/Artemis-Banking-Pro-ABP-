using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Transactions
{
    public sealed class CashAdvanceValidationServices : ICashAdvanceValidationServices
    {
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly ILogger<CashAdvanceValidationServices> _logger;
        private readonly IUserManagementService _userManagementService;

        public CashAdvanceValidationServices(
            ICreditCardsRepository creditCardsRepository,
            ISavingsAccountsRepository savingsAccountsRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ILogger<CashAdvanceValidationServices> _logger,
            IUserManagementService userManagementService)
        {
            _creditCardsRepository = creditCardsRepository;
            _savingsAccountsRepository = savingsAccountsRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            this._logger = _logger;
            _userManagementService = userManagementService;
        }

        public async Task<ValidationResult<(CreditCard Card, SavingsAccount Account, decimal InterestAmount, decimal TotalCharged)>> ValidateCashAdvanceAsync(
            CashAdvanceRequestDto dto, 
            string clientId)
        {
            _logger.LogInformation("Iniciando validación de avance de efectivo para el cliente {ClientId}", clientId);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Validación fallida: el monto solicitado {Amount} es inválido", dto.Amount);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.AmountInvalid);
            }

            var userValidation = await _userManagementService.ValidateUserExistsByIdAsync(clientId);
            if (!userValidation.Exists || !userValidation.IsActive)
            {
                _logger.LogWarning("Validación fallida: el cliente {ClientId} no existe o no está activo en el sistema", clientId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.AccountNotActive);
            }

            var card = await _creditCardsRepository.GetFirstAsync(c => c.Id == dto.CreditCardId && c.CustomerId == clientId);
            if (card is null)
            {
                _logger.LogWarning("Validación fallida: la tarjeta de crédito ID {CardId} no existe o no pertenece al cliente {ClientId}", dto.CreditCardId, clientId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.CardNotActive);
            }

            var interestRate = DomainConstants.CashAdvanceInterestRate;
            var interestAmount = dto.Amount * interestRate;
            var totalCharged = dto.Amount + interestAmount;

            if (card.Status != CreditCardStatus.Activa)
            {
                _logger.LogWarning("Validación fallida: la tarjeta de crédito ****{LastFour} está inactiva o cancelada", card.LastFourDigits);
                await RegisterRejectedConsumptionAsync(card.Id, totalCharged, RejectionReason.TarjetaCancelada, clientId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.CardNotActive);
            }

            if (card.IsExpired)
            {
                _logger.LogWarning("Validación fallida: la tarjeta de crédito ****{LastFour} está vencida", card.LastFourDigits);
                await RegisterRejectedConsumptionAsync(card.Id, totalCharged, RejectionReason.TarjetaVencida, clientId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.CardExpired);
            }

            var account = await _savingsAccountsRepository.GetFirstAsync(a => a.Id == dto.SavingsAccountId && a.CustomerId == clientId);
            if (account is null || !account.IsActive)
            {
                _logger.LogWarning("Validación fallida: la cuenta de ahorros destino ID {AccountId} no existe o no está activa", dto.SavingsAccountId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.AccountNotActive);
            }

            if (totalCharged > card.AvailableCredit)
            {
                _logger.LogWarning("Validación fallida: crédito disponible insuficiente (Disponible: {Available}, Total a cargar: {Total})", card.AvailableCredit, totalCharged);
                await RegisterRejectedConsumptionAsync(card.Id, totalCharged, RejectionReason.CreditoInsuficiente, clientId);
                return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Failure(CashAdvanceError.InsufficientCredit);
            }

            _logger.LogInformation("Validación de avance de efectivo exitosa para tarjeta ****{LastFour} y cuenta ****{LastFourAccount}", card.LastFourDigits, account.AccountNumber.Substring(Math.Max(0, account.AccountNumber.Length - 4)));
            return ValidationResult<(CreditCard, SavingsAccount, decimal, decimal)>.Success((card, account, interestAmount, totalCharged));
        }

        private async Task RegisterRejectedConsumptionAsync(int cardId, decimal amount, RejectionReason reason, string clientId)
        {
            try
            {
                _logger.LogInformation("Registrando intento de consumo RECHAZADO para la tarjeta ID {CardId} debido a {Reason}", cardId, reason);
                var consumption = new CardConsumption
                {
                    CreditCardId = cardId,
                    Amount = amount,
                    Origin = ConsumptionOrigin.Avance,
                    CommerceName = "AVANCE",
                    Status = ConsumptionStatus.Rechazado,
                    RejectionReason = reason,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _cardConsumptionRepository.AddAsync(consumption);
                await _cardConsumptionRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el consumo rechazado para la tarjeta ID {CardId}", cardId);
            }
        }
    }
}
