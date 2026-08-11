using Artemis_Banking_Pro.Core.Application.Contracts.Commerces;
using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Commerces;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.CommercesErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Artemis_Banking_Pro.Core.Application.Services.Commerces
{
    public sealed class HermesPayServices : IHermesPayServices
    {
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICommercePaymentRepository _commercePaymentRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IEmailServices _emailServices;
        private readonly ICvcHasher _cvcHasher;
        private readonly ILogger<HermesPayServices> _logger;

        public HermesPayServices(
            ICreditCardsRepository creditCardsRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository,
            ICommercePaymentRepository commercePaymentRepository,
            IUserManagementService userManagementService,
            IEmailServices emailServices,
            ICvcHasher cvcHasher,
            ILogger<HermesPayServices> logger)
        {
            _creditCardsRepository = creditCardsRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
            _commercePaymentRepository = commercePaymentRepository;
            _userManagementService = userManagementService;
            _emailServices = emailServices;
            _cvcHasher = cvcHasher;
            _logger = logger;
        }

        public async Task<ValidationResult> ProcessPaymentAsync(Commerce commerce, ProcessPaymentDto dto)
        {
            //Nunca se registra el número completo ni el CVC: solo los últimos cuatro dígitos
            _logger.LogInformation(
                "Procesando pago Hermes Pay del comercio {CommerceId} con la tarjeta terminada en {LastFour}",
                commerce.Id, LastFourOf(dto.CardNumber));

            var cardValidation = await ValidateCardAsync(dto);
            if (!cardValidation.IsValid) return cardValidation;

            var card = cardValidation.Value!;

            var accountValidation = await ValidateCommerceAccountAsync(commerce);
            if (!accountValidation.IsValid) return accountValidation;

            var commerceAccount = accountValidation.Value!;

            //Crédito disponible = límite - deuda actual. Si no alcanza, el intento queda
            //registrado como RECHAZADO sin tocar balances ni deudas.
            if (dto.TransactionAmount > card.AvailableCredit)
            {
                await RegisterRejectedConsumptionAsync(card, commerce, dto.TransactionAmount);
                return ValidationResult.Failure(HermesPayError.AmountExceedsAvailableCredit);
            }

            try
            {
                var approved = await ApproveePaymentAsync(card, commerce, commerceAccount, dto.TransactionAmount);

                //El fallo de correo no revierte un pago aprobado
                if (!await NotifyAsync(card, commerce, dto.TransactionAmount, approved))
                    return ValidationResult.Failure(HermesPayError.PaymentProcessedWithoutNotification);

                return ValidationResult.Success();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Error al procesar el pago Hermes Pay del comercio {CommerceId}", commerce.Id);

                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        #region Validaciones

        private async Task<ValidationResult<CreditCard>> ValidateCardAsync(ProcessPaymentDto dto)
        {
            var card = await _creditCardsRepository.GetFirstAsync(
                entity => entity.CardNumber == dto.CardNumber);

            if (card is null)
                return ValidationResult<CreditCard>.Failure(HermesPayError.NonExistsCreditCard);

            if (card.Status != CreditCardStatus.Activa)
                return ValidationResult<CreditCard>.Failure(HermesPayError.CreditCardIsNotActive);

            if (card.IsExpired)
                return ValidationResult<CreditCard>.Failure(HermesPayError.CreditCardExpired);

            //Expiración y CVC comparten mensaje: distinguirlos ayudaría a adivinar los datos
            if (!MatchesExpiration(card, dto) || !_cvcHasher.Verify(dto.Cvc, card.CvcHash))
                return ValidationResult<CreditCard>.Failure(HermesPayError.InvalidCardCredentials);

            return ValidationResult<CreditCard>.Success(card);
        }

        private static bool MatchesExpiration(CreditCard card, ProcessPaymentDto dto)
        {
            if (!int.TryParse(dto.MonthExpirationCard, out var month)) return false;
            if (!int.TryParse(dto.YearExpirationCard, out var year)) return false;

            //El año puede llegar en formato YYYY o AA
            if (year < 100) year += 2000;

            return card.ExpirationDate.Month == month && card.ExpirationDate.Year == year;
        }

        private async Task<ValidationResult<SavingsAccount>> ValidateCommerceAccountAsync(Commerce commerce)
        {
            if (!commerce.HasAssociatedUser)
                return ValidationResult<SavingsAccount>.Failure(CommerceError.CommerceWithoutAssociatedUser);

            var account = await _savingsAccountsRepository.GetFirstAsync(entity =>
                entity.CustomerId == commerce.AssociatedUserId &&
                entity.AccountType == SavingsAccountType.Principal &&
                entity.Status == SavingsAccountStatus.Activa);

            return account is null
                ? ValidationResult<SavingsAccount>.Failure(HermesPayError.CommerceWithoutActivePrimaryAccount)
                : ValidationResult<SavingsAccount>.Success(account);
        }

        #endregion

        #region Procesamiento

        private async Task<CommercePayment> ApproveePaymentAsync(
            CreditCard card, Commerce commerce, SavingsAccount commerceAccount, decimal amount)
        {
            var now = DateTimeOffset.UtcNow;

            card.OwedAmount += amount;
            commerceAccount.Balance += amount;

            await _creditCardsRepository.UpdateAsync(card);
            await _savingsAccountsRepository.UpdateAsync(commerceAccount);

            var consumption = await _cardConsumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                Amount = amount,
                Origin = ConsumptionOrigin.Comercio,
                CommerceId = commerce.Id,
                CommerceName = commerce.Name,
                Status = ConsumptionStatus.Aprobado,
                CreatedAt = now,
                CreateByUserId = card.CustomerId
            });

            var transaction = await _transactionRepository.AddAsync(new Transaction
            {
                SavingsAccountId = commerceAccount.Id,
                Amount = amount,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.PagoHermesPay,
                //El documento exige los últimos cuatro dígitos como origen, nunca el número
                Origin = card.LastFourDigits,
                Beneficiary = commerceAccount.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = commerce.AssociatedUserId!,
                Channel = ChannelPayment.HermesPay,
                CreatedAt = now,
                CreateByUserId = commerce.AssociatedUserId!
            });

            var payment = await _commercePaymentRepository.AddAsync(new CommercePayment
            {
                CommerceId = commerce.Id,
                CreditCardId = card.Id,
                CardLastFourDigits = card.LastFourDigits,
                Amount = amount,
                CardConsumptionId = consumption.Id,
                TransactionId = transaction.Id,
                Status = ConsumptionStatus.Aprobado,
                CreatedAt = now,
                CreateByUserId = commerce.AssociatedUserId!
            });

            //Un solo SaveChanges: deuda, balance, consumo, transacción y pago se aplican juntos
            await _commercePaymentRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Pago Hermes Pay aprobado. Comercio {CommerceId}, tarjeta terminada en {LastFour}, monto {Monto}",
                commerce.Id, card.LastFourDigits, amount);

            return payment;
        }

        //El intento rechazado se conserva como evidencia: no aumenta la deuda de la tarjeta ni
        //acredita fondos al comercio, y no genera transacción de crédito.
        private async Task RegisterRejectedConsumptionAsync(
            CreditCard card, Commerce commerce, decimal amount)
        {
            var now = DateTimeOffset.UtcNow;

            var consumption = await _cardConsumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                Amount = amount,
                Origin = ConsumptionOrigin.Comercio,
                CommerceId = commerce.Id,
                CommerceName = commerce.Name,
                Status = ConsumptionStatus.Rechazado,
                RejectionReason = RejectionReason.CreditoInsuficiente,
                CreatedAt = now,
                CreateByUserId = card.CustomerId
            });

            await _commercePaymentRepository.AddAsync(new CommercePayment
            {
                CommerceId = commerce.Id,
                CreditCardId = card.Id,
                CardLastFourDigits = card.LastFourDigits,
                Amount = amount,
                CardConsumptionId = consumption.Id,
                TransactionId = null,
                Status = ConsumptionStatus.Rechazado,
                CreatedAt = now,
                CreateByUserId = commerce.AssociatedUserId ?? card.CustomerId
            });

            await _commercePaymentRepository.SaveChangesAsync();

            _logger.LogWarning(
                "Pago Hermes Pay rechazado por crédito insuficiente. Comercio {CommerceId}, tarjeta terminada en {LastFour}",
                commerce.Id, card.LastFourDigits);
        }

        #endregion

        #region Notificaciones

        private async Task<bool> NotifyAsync(
            CreditCard card, Commerce commerce, decimal amount, CommercePayment payment)
        {
            var customer = await _userManagementService.GetUserByIdAsync(card.CustomerId);
            var moment = payment.CreatedAt.ToLocalTime();

            var clientNotified = customer is not null && await _emailServices.SendNotification(new MessageDto
            {
                To = customer.Email,
                Subject = $"Consumo realizado con la tarjeta {card.LastFourDigits}",
                Message = BuildClientBody(
                    $"{customer.Name} {customer.LastName}".Trim(),
                    card.LastFourDigits, commerce.Name, amount, moment)
            });

            var commerceNotified = await _emailServices.SendNotification(new MessageDto
            {
                To = commerce.Email,
                Subject = $"Pago recibido a través de tarjeta {card.LastFourDigits}",
                Message = BuildCommerceBody(commerce.Name, card.LastFourDigits, amount, moment)
            });

            return clientNotified && commerceNotified;
        }

        private static string BuildClientBody(
            string customerName, string lastFour, string commerceName, decimal amount, DateTimeOffset moment)
            => $"""
                Hola {customerName},

                Se ha realizado un consumo con su tarjeta terminada en {lastFour}.

                Comercio: {commerceName}
                Monto: RD${amount.ToString("N2", CultureInfo.InvariantCulture)}
                Fecha y hora: {moment:dd/MM/yyyy hh:mm tt}

                Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
                """;

        private static string BuildCommerceBody(
            string commerceName, string lastFour, decimal amount, DateTimeOffset moment)
            => $"""
                Hola {commerceName},

                Ha recibido un nuevo pago mediante Hermes Pay.

                Tarjeta terminada en: {lastFour}
                Monto recibido: RD${amount.ToString("N2", CultureInfo.InvariantCulture)}
                Fecha y hora: {moment:dd/MM/yyyy hh:mm tt}

                Este mensaje sirve como constancia del pago recibido.
                """;

        private static string LastFourOf(string cardNumber)
            => cardNumber.Length <= 4 ? cardNumber : cardNumber[^4..];

        #endregion
    }
}
