using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.Transactions
{
    public sealed class CashAdvanceServices : ICashAdvanceServices
    {
        private readonly ICashAdvanceValidationServices _validationServices;
        private readonly ICashAdvanceRepository _cashAdvanceRepository;
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IEmailServices _emailServices;
        private readonly IMapper _mapper;
        private readonly ILogger<CashAdvanceServices> _logger;
        private readonly IUserManagementService _userManagementService;

        public CashAdvanceServices(
            ICashAdvanceValidationServices validationServices,
            ICashAdvanceRepository cashAdvanceRepository,
            ICreditCardsRepository creditCardsRepository,
            ISavingsAccountsRepository savingsAccountsRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ITransactionRepository transactionRepository,
            IEmailServices emailServices,
            IMapper mapper,
            ILogger<CashAdvanceServices> logger,
            IUserManagementService userManagementService)
        {
            _validationServices = validationServices;
            _cashAdvanceRepository = cashAdvanceRepository;
            _creditCardsRepository = creditCardsRepository;
            _savingsAccountsRepository = savingsAccountsRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            _transactionRepository = transactionRepository;
            _emailServices = emailServices;
            _mapper = mapper;
            _logger = logger;
            _userManagementService = userManagementService;
        }

        public async Task<ValidationResult<CashAdvanceDto>> ProcessCashAdvanceAsync(
            CashAdvanceRequestDto dto, 
            string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de avance de efectivo para el cliente {ClientId} por monto RD${Amount}", clientId, dto.Amount);

            var validation = await _validationServices.ValidateCashAdvanceAsync(dto, clientId);
            if (!validation.IsValid)
            {
                return ValidationResult<CashAdvanceDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (card, account, interestAmount, totalCharged) = validation.Value;

                card.OwedAmount += totalCharged;
                account.Balance += dto.Amount;

                await _creditCardsRepository.UpdateAsync(card);
                await _savingsAccountsRepository.UpdateAsync(account);

                var consumption = new CardConsumption
                {
                    CreditCardId = card.Id,
                    Amount = totalCharged,
                    Origin = ConsumptionOrigin.Avance,
                    CommerceName = "AVANCE",
                    Status = ConsumptionStatus.Aprobado,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var creditTx = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.Credito,
                    OperationType = OperationType.AvanceEfectivo,
                    Origin = card.LastFourDigits,
                    Beneficiary = account.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    PerformedByUserId = clientId,
                    Channel = ChannelPayment.Cliente,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var cashAdvance = new CashAdvance
                {
                    CreditCardId = card.Id,
                    SavingsAccountId = account.Id,
                    RequestedAmount = dto.Amount,
                    InterestRate = DomainConstants.CashAdvanceInterestRate,
                    InterestAmount = interestAmount,
                    TotalCharged = totalCharged,
                    CardConsumptionId = 0,
                    CardConsumption = consumption,
                    TransactionId = 0,
                    Transaction = creditTx,
                    CreateByUserId = clientId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _cardConsumptionRepository.AddAsync(consumption);
                await _transactionRepository.AddAsync(creditTx);
                await _cashAdvanceRepository.AddAsync(cashAdvance);

                var saveResult = await _cashAdvanceRepository.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    _logger.LogWarning("Error de persistencia al guardar el avance de efectivo para el cliente {ClientId}", clientId);
                    return ValidationResult<CashAdvanceDto>.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Avance de efectivo procesado exitosamente para el cliente {ClientId}. ID de Avance: {CashAdvanceId}", clientId, cashAdvance.Id);

                var emailSent = await SendCashAdvanceEmailAsync(card.LastFourDigits, account.AccountNumber, dto.Amount, interestAmount, totalCharged, clientId);

                // Asignamos las referencias en memoria para que AutoMapper resuelva las propiedades de navegación de la tarjeta y cuenta
                cashAdvance.CreditCard = card;
                cashAdvance.SavingsAccount = account;

                var resultDto = _mapper.Map<CashAdvanceDto>(cashAdvance);
                return ValidationResult<CashAdvanceDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar el avance de efectivo para el cliente {ClientId}", clientId);
                return ValidationResult<CashAdvanceDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        private async Task<bool> SendCashAdvanceEmailAsync(
            string lastFourCard, 
            string accountNumber, 
            decimal requestedAmount, 
            decimal interestAmount, 
            decimal totalCharged, 
            string clientId)
        {
            var user = await _userManagementService.GetUserByIdAsync(clientId);
            var email = user?.Email ?? $"{clientId}@artemis.com";
            var lastFourAccount = accountNumber.Length >= 4 
                ? accountNumber.Substring(accountNumber.Length - 4) 
                : accountNumber;

            try
            {
                _logger.LogInformation("Enviando correo de notificación de avance de efectivo al cliente {ClientId}", clientId);
                var sent = await _emailServices.SendNotification(new MessageDto
                {
                    To = email,
                    Subject = $"Avance de efectivo desde la tarjeta {lastFourCard}",
                    Message = $"Se ha realizado un avance de efectivo por RD${requestedAmount:N2}.\n\nDetalles:\n- Interés aplicado: RD${interestAmount:N2} (6.25%)\n- Total cargado a tarjeta: RD${totalCharged:N2}\n- Cuenta de ahorro destino: ****{lastFourAccount}\n- Fecha: {DateTimeOffset.UtcNow}"
                });
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar notificación por correo de avance de efectivo.");
                return false;
            }
        }
    }
}
