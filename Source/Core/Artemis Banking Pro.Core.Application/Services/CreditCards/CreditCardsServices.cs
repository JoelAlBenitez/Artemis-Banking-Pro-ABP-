using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.CreditCardsErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.CreditCards
{
    public sealed class CreditCardsServices :
        GenericServices<CreditCardAssignmentDto, CreditCardDto, int, CreditCard>,
        ICreditCardsServices
    {
        private const string ExpirationDateFormat = "MM/yy";

        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly ICreditCardsValidationServices _creditCardsValidationServices;
        private readonly ICardNumberGenerator _cardNumberGenerator;
        private readonly ICvcHasher _cvcHasher;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<CreditCardsServices> _logger;

        public CreditCardsServices(
            ICreditCardsRepository creditCardsRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ICreditCardsValidationServices creditCardsValidationServices,
            ICardNumberGenerator cardNumberGenerator,
            ICvcHasher cvcHasher,
            IEmailServices emailServices,
            IMapper mapper,
            ILogger<CreditCardsServices> logger)
            : base(creditCardsRepository, mapper, logger)
        {
            _creditCardsRepository = creditCardsRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            _creditCardsValidationServices = creditCardsValidationServices;
            _cardNumberGenerator = cardNumberGenerator;
            _cvcHasher = cvcHasher;
            _emailServices = emailServices;
            _logger = logger;
        }

        //La asignación exige el administrador responsable para la auditoría de la tarjeta.
        //Cuando exista ICurrentUserServices (project Identity) este override resolverá el usuario
        //autenticado e invocará AssignCreditCardAsync sin recibirlo desde la capa de presentación.
        public override Task<ValidationResult> CreateAsync(CreditCardAssignmentDto dto)
        {
            _logger.LogWarning("Intento de asignar una tarjeta de crédito sin el administrador responsable");
            return Task.FromResult(ValidationResult.Failure(CreditCardError.AdminUserRequired));
        }

        //La única modificación permitida sobre una tarjeta es su límite de crédito y exige
        //el administrador responsable, por lo que se resuelve con EditCreditCardLimitAsync.
        public override Task<ValidationResult> UpdateAsync(int tkey, CreditCardAssignmentDto dto)
        {
            _logger.LogWarning("Intento de modificar la tarjeta con ID {CreditCardId} sin el administrador responsable", tkey);
            return Task.FromResult(ValidationResult.Failure(CreditCardError.AdminUserRequired));
        }

        public async Task<ValidationResult<PagedResult<CreditCardDto>>> GetPagedCreditCardsAsync(
            CreditCardFilterDto filter, string? customerId)
        {
            try
            {
                _logger.LogInformation("Recuperando el listado de tarjetas de crédito. Página {Pagina}, estado {Estado}",
                    filter.Page, filter.Status);

                var result = await _creditCardsRepository.GetPagedCreditCardsAsync(
                    filter.Page,
                    DomainConstants.DefaultPageSize,
                    ToCreditCardStatus(filter.Status),
                    customerId);

                if (!string.IsNullOrWhiteSpace(customerId) && result.TotalRecords == 0)
                {
                    _logger.LogWarning("El cliente consultado no tiene tarjetas de crédito registradas");
                    return ValidationResult<PagedResult<CreditCardDto>>.Failure(CreditCardError.NonExistsCreditCards);
                }

                //El nombre del cliente de cada tarjeta proviene del project Identity y se completa
                //cuando su servicio de consulta de usuarios esté disponible.

                var items = _mapper.Map<IReadOnlyCollection<CreditCardDto>>(result.Items);
                var paged = new PagedResult<CreditCardDto>(
                    items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<CreditCardDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el listado de tarjetas de crédito");
                return ValidationResult<PagedResult<CreditCardDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<PagedResult<CardConsumptionDto>>> GetPagedConsumptionsAsync(
            int creditCardId, int page)
        {
            try
            {
                _logger.LogInformation("Recuperando los consumos de la tarjeta con ID {CreditCardId}. Página {Pagina}",
                    creditCardId, page);

                var creditCard = await _creditCardsRepository.GetByIdAsync(creditCardId);
                if (creditCard is null)
                {
                    _logger.LogWarning("Tarjeta de crédito con ID {CreditCardId} inexistente", creditCardId);
                    return ValidationResult<PagedResult<CardConsumptionDto>>.Failure(CreditCardError.NonExistsCreditCard);
                }

                var result = await _cardConsumptionRepository.GetAllAsync(
                    page,
                    DomainConstants.DefaultPageSize,
                    consumption => consumption.CreditCardId == creditCardId,
                    query => query.OrderByDescending(consumption => consumption.CreatedAt));

                var items = _mapper.Map<IReadOnlyCollection<CardConsumptionDto>>(result.Items);
                var paged = new PagedResult<CardConsumptionDto>(
                    items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<CardConsumptionDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los consumos de la tarjeta con ID {CreditCardId}", creditCardId);
                return ValidationResult<PagedResult<CardConsumptionDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<EditCardLimitDto>> GetCreditCardForEditLimitAsync(int creditCardId)
        {
            var validation = await _creditCardsValidationServices.ValidateActiveCreditCardAsync(creditCardId);
            if (!validation.IsValid)
            {
                return ValidationResult<EditCardLimitDto>.Failure(validation.Errors.ToList());
            }

            return ValidationResult<EditCardLimitDto>.Success(_mapper.Map<EditCardLimitDto>(validation.Value!));
        }

        public async Task<ValidationResult<CreditCardAssignedDto>> AssignCreditCardAsync(
            CreditCardAssignmentDto dto, string adminUserId)
        {
            _logger.LogInformation("Inicio de la asignación de una tarjeta de crédito al cliente {CustomerId}",
                dto.CustomerId);

            if (string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("Asignación de tarjeta rechazada: no se recibió el administrador responsable");
                return ValidationResult<CreditCardAssignedDto>.Failure(CreditCardError.AdminUserRequired);
            }

            var validation = await _creditCardsValidationServices.ValidateAssignmentAsync(dto);
            if (!validation.IsValid)
            {
                return ValidationResult<CreditCardAssignedDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var cardNumber = await _cardNumberGenerator.GenerateUniqueCardNumberAsync();
                if (cardNumber is null)
                {
                    _logger.LogError("No fue posible generar un número de tarjeta único para el cliente {CustomerId}",
                        dto.CustomerId);

                    return ValidationResult<CreditCardAssignedDto>.Failure(CreditCardError.FailedGenerateCardNumber);
                }

                var assignedAt = DateTimeOffset.UtcNow;
                var creditCard = _mapper.Map<CreditCard>(dto);

                creditCard.CardNumber = cardNumber;
                creditCard.CvcHash = _cvcHasher.Hash(_cvcHasher.GenerateCvc());
                creditCard.ExpirationDate = assignedAt.AddYears(DomainConstants.CardExpirationYears);
                creditCard.AssignedByAdminId = adminUserId;
                creditCard.CreatedAt = assignedAt;
                creditCard.CreateByUserId = adminUserId;

                await _creditCardsRepository.AddAsync(creditCard);
                var result = await _creditCardsRepository.SaveChangesAsync();
                if(result <= 0)
                {
                    _logger.LogWarning("La tarjeta de credito terminada {LastFourDigits} en el intento de " +
                        " asignación al cliente {CustomerId}, fallo en su asignación", creditCard.LastFourDigits, creditCard.CustomerId);
                    return ValidationResult<CreditCardAssignedDto>.Failure(GeneralError.UnexpectedError) ;
                }
                _logger.LogInformation("Tarjeta de crédito terminada en {LastFourDigits} asignada al cliente {CustomerId}",
                    creditCard.LastFourDigits, creditCard.CustomerId);

                var assigned = new CreditCardAssignedDto
                {
                    CustomerId = creditCard.CustomerId,
                    LastFourDigits = creditCard.LastFourDigits,
                    CreditLimit = creditCard.CreditLimit,
                    ExpirationDate = creditCard.ExpirationDate.ToString(ExpirationDateFormat),
                    AssignedAt = assignedAt
                };

                return ValidationResult<CreditCardAssignedDto>.Success(assigned);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar la tarjeta de crédito al cliente {CustomerId}", dto.CustomerId);
                return ValidationResult<CreditCardAssignedDto>.Failure(CreditCardError.FailedProcessCreditCard);
            }
        }

        public async Task<ValidationResult<CardLimitUpdatedDto>> EditCreditCardLimitAsync(
            EditCardLimitDto dto, string adminUserId)
        {
            _logger.LogInformation("Inicio de la modificación del límite de la tarjeta con ID {CreditCardId}", dto.Id);

            if (string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("Modificación de límite rechazada: no se recibió el administrador responsable");
                return ValidationResult<CardLimitUpdatedDto>.Failure(CreditCardError.AdminUserRequired);
            }

            var validation = await _creditCardsValidationServices.ValidateLimitEditionAsync(dto);
            if (!validation.IsValid)
            {
                return ValidationResult<CardLimitUpdatedDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var creditCard = validation.Value!;
                var modifiedAt = DateTimeOffset.UtcNow;

                creditCard.CreditLimit = dto.CreditLimit;
                creditCard.ModifiedAt = modifiedAt;
                creditCard.LastModifiedByIdUser = adminUserId;

                await _creditCardsRepository.UpdateAsync(creditCard);
                await _creditCardsRepository.SaveChangesAsync();

                _logger.LogInformation("Límite actualizado para la tarjeta terminada en {LastFourDigits}",
                    creditCard.LastFourDigits);

                var limitUpdated = new CardLimitUpdatedDto
                {
                    CustomerId = creditCard.CustomerId,
                    LastFourDigits = creditCard.LastFourDigits,
                    CreditLimit = creditCard.CreditLimit,
                    ModifiedAt = modifiedAt
                };

                return ValidationResult<CardLimitUpdatedDto>.Success(limitUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar el límite de la tarjeta con ID {CreditCardId}", dto.Id);
                return ValidationResult<CardLimitUpdatedDto>.Failure(CreditCardError.FailedProcessCreditCard);
            }
        }

        public async Task<ValidationResult> CancelCreditCardAsync(int creditCardId, string adminUserId)
        {
            _logger.LogInformation("Inicio de la cancelación de la tarjeta con ID {CreditCardId}", creditCardId);

            if (string.IsNullOrWhiteSpace(adminUserId))
            {
                _logger.LogWarning("Cancelación rechazada: no se recibió el administrador responsable");
                return ValidationResult.Failure(CreditCardError.AdminUserRequired);
            }

            var validation = await _creditCardsValidationServices.ValidateCancellationAsync(creditCardId);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                var creditCard = validation.Value!;

                creditCard.Status = CreditCardStatus.Cancelada;
                creditCard.ModifiedAt = DateTimeOffset.UtcNow;
                creditCard.LastModifiedByIdUser = adminUserId;

                await _creditCardsRepository.UpdateAsync(creditCard);
                await _creditCardsRepository.SaveChangesAsync();

                _logger.LogInformation("Tarjeta terminada en {LastFourDigits} cancelada. El historial de consumos se conserva",
                    creditCard.LastFourDigits);

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar la tarjeta con ID {CreditCardId}", creditCardId);
                return ValidationResult.Failure(CreditCardError.FailedProcessCreditCard);
            }
        }

        public async Task<ValidationResult> SendCreditCardAssignedNotificationAsync(
            CreditCardAssignedDto assigned, string customerEmail, string customerFullName)
        {
            var message = new MessageDto
            {
                To = customerEmail,
                Subject = "Nueva tarjeta de crédito asignada",
                Message = BuildCreditCardAssignedBody(assigned, customerFullName)
            };

            var sent = await _emailServices.SendNotification(message);

            if (!sent)
            {
                _logger.LogWarning("No fue posible enviar el correo de asignación de la tarjeta terminada en {LastFourDigits}. La operación no se revierte",
                    assigned.LastFourDigits);

                return ValidationResult.Failure(CreditCardError.CreditCardCreatedWithoutNotification);
            }

            return ValidationResult.Success();
        }

        public async Task<ValidationResult> SendCardLimitUpdatedNotificationAsync(
            CardLimitUpdatedDto limitUpdated, string customerEmail, string customerFullName)
        {
            var message = new MessageDto
            {
                To = customerEmail,
                Subject = "Modificación de límite de tarjeta",
                Message = BuildCardLimitUpdatedBody(limitUpdated, customerFullName)
            };

            var sent = await _emailServices.SendNotification(message);

            if (!sent)
            {
                _logger.LogWarning("No fue posible enviar el correo de modificación de límite de la tarjeta terminada en {LastFourDigits}. La operación no se revierte",
                    limitUpdated.LastFourDigits);

                return ValidationResult.Failure(CreditCardError.CreditLimitUpdatedWithoutNotification);
            }

            return ValidationResult.Success();
        }

        private static string BuildCreditCardAssignedBody(CreditCardAssignedDto assigned, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               "<p>Se ha asignado una nueva tarjeta de crédito a su cuenta.</p>" +
               $"<p>Tarjeta terminada en: {assigned.LastFourDigits}<br/>" +
               $"Límite aprobado: RD${assigned.CreditLimit:N2}<br/>" +
               $"Fecha de expiración: {assigned.ExpirationDate}<br/>" +
               $"Fecha de asignación: {assigned.AssignedAt:dd/MM/yyyy}</p>" +
               "<p>Por seguridad, no comparta la información de su tarjeta con terceros.</p>";

        private static string BuildCardLimitUpdatedBody(CardLimitUpdatedDto limitUpdated, string customerFullName)
            => $"<p>Hola {customerFullName},</p>" +
               $"<p>El límite de su tarjeta de crédito terminada en {limitUpdated.LastFourDigits} ha sido actualizado.</p>" +
               $"<p>Nuevo límite aprobado: RD${limitUpdated.CreditLimit:N2}<br/>" +
               $"Fecha de modificación: {limitUpdated.ModifiedAt:dd/MM/yyyy}</p>" +
               "<p>Si usted no reconoce esta modificación, comuníquese con la entidad bancaria.</p>";

        private static CreditCardStatus? ToCreditCardStatus(CreditCardStatusFilter filter)
            => filter switch
            {
                CreditCardStatusFilter.Activas => CreditCardStatus.Activa,
                CreditCardStatusFilter.Canceladas => CreditCardStatus.Cancelada,
                _ => null
            };
    }
}
