using Artemis_Banking_Pro.Core.Application.Contracts.CreditCards;
using Artemis_Banking_Pro.Core.Application.Contracts.Debts;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Application.DTOs.Users;
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
        private readonly IDebtCalculator _debtCalculator;
        private readonly IUserManagementService _userManagementService;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<CreditCardsServices> _logger;

        public CreditCardsServices(
            ICreditCardsRepository creditCardsRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            ICreditCardsValidationServices creditCardsValidationServices,
            ICardNumberGenerator cardNumberGenerator,
            ICvcHasher cvcHasher,
            IDebtCalculator debtCalculator,
            IUserManagementService userManagementService,
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
            _debtCalculator = debtCalculator;
            _userManagementService = userManagementService;
            _emailServices = emailServices;
            _logger = logger;
        }

        #region query methods
        public async Task<ValidationResult<PagedResult<CreditCardDto>>> GetPagedCreditCardsAsync(
            CreditCardFilterDto filter)
        {
            try
            {
                _logger.LogInformation("Recuperando el listado de tarjetas de crédito. Página {Pagina}, estado {Estado}",
                    filter?.Page, filter?.Status);

                var queryValidation = await _creditCardsValidationServices.ValidateCustomerCardsQueryAsync(filter!);
                if (!queryValidation.IsValid)
                {
                    return ValidationResult<PagedResult<CreditCardDto>>.Failure(queryValidation.Errors.ToList());
                }

                var customerId = queryValidation.Value;

                var result = await _creditCardsRepository.GetPagedCreditCardsAsync(
                    filter!.Page,
                    filter.PageSize,
                    ToCreditCardStatus(filter.Status),
                    customerId);

                if (!string.IsNullOrWhiteSpace(customerId) && result.TotalRecords == 0)
                {
                    _logger.LogWarning("El cliente consultado no tiene tarjetas de crédito registradas");
                    return ValidationResult<PagedResult<CreditCardDto>>.Failure(CreditCardError.NonExistsCreditCards);
                }

                var items = _mapper.Map<IReadOnlyCollection<CreditCardDto>>(result.Items);

                await FillCustomerDataAsync(items);

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

        //El listado genérico del módulo muestra activas y canceladas sin filtro de cédula
        public override async Task<ValidationResult<PagedResult<CreditCardDto>>> GetAllAsync(int page, int pageSize)
            => await GetPagedCreditCardsAsync(new CreditCardFilterDto
            {
                Status = CreditCardStatusFilter.Todas,
                Page = page
            });

        public override async Task<ValidationResult<CreditCardDto>> GetByIdAsync(int creditCardId)
        {
            var result = await base.GetByIdAsync(creditCardId);

            if (result.IsValid)
            {
                await FillCustomerDataAsync(new[] { result.Value! });
            }

            return result;
        }

        public async Task<ValidationResult<ClientsForCreditCardAssignmentDto>> GetCustomersForAssignmentAsync(
            string? idCard)
        {
            try
            {
                _logger.LogInformation("Recuperando los clientes activos elegibles para asignarles una tarjeta de crédito");

                var averageDebt = await _debtCalculator.GetAverageDebtAsync();

                List<ClientSummaryDto> customers;

                if (string.IsNullOrWhiteSpace(idCard))
                {
                    customers = await _userManagementService.GetActiveClientsAsync();
                }
                else
                {
                    var customer = await _userManagementService.GetClientByIdCardAsync(idCard);

                    if (customer is null)
                    {
                        _logger.LogWarning("No existe un cliente activo registrado con la cédula {IdCard}", idCard);
                        return ValidationResult<ClientsForCreditCardAssignmentDto>.Failure(
                            CreditCardError.NonExistsCustomerByIdCard);
                    }

                    customers = new List<ClientSummaryDto> { customer };
                }

                //Una sola pasada por los productos activos: la deuda de todos los clientes de la
                //pantalla se resuelve con dos consultas, no con dos por cliente.
                var debts = await _debtCalculator.GetCustomersDebtAsync(
                    customers.Select(customer => customer.Id).ToList());

                var clients = customers
                    .Select(customer => new ClientCreditCardDto
                    {
                        Id = customer.Id,
                        IdCard = customer.IDCARD,
                        FullName = customer.FullName,
                        Email = customer.Email,
                        TotalDebtAmount = debts.TryGetValue(customer.Id, out var debt) ? debt : 0m
                    })
                    .ToList();

                return ValidationResult<ClientsForCreditCardAssignmentDto>.Success(
                    new ClientsForCreditCardAssignmentDto
                    {
                        AverageDebt = averageDebt,
                        Clients = clients
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los clientes activos para la asignación de una tarjeta de crédito");
                return ValidationResult<ClientsForCreditCardAssignmentDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<PagedResult<CardConsumptionDto>>> GetPagedConsumptionsAsync(
            int creditCardId, int page, int pageSize = DomainConstants.DefaultPageSize)
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

                //Aprobados y rechazados: el historial conserva los intentos denegados
                var result = await _cardConsumptionRepository.GetAllAsync(
                    page,
                    pageSize,
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
        #endregion

        #region write methods
        public async Task<ValidationResult> AssignCreditCardAsync(
            CreditCardAssignmentDto dto)
        {
            _logger.LogInformation("Inicio de la asignación de una tarjeta de crédito al cliente {CustomerId}",
                dto?.CustomerId);

            var adminValidation = _creditCardsValidationServices.ValidateAdministratorInSession();
            if (!adminValidation.IsValid)
            {
                return ValidationResult.Failure(adminValidation.Errors.ToList());
            }

            var adminUserId = adminValidation.Value!;

            var validation = await _creditCardsValidationServices.ValidateAssignmentAsync(dto!);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                var cardNumber = await _cardNumberGenerator.GenerateUniqueCardNumberAsync();
                if (cardNumber is null)
                {
                    _logger.LogError("No fue posible generar un número de tarjeta único para el cliente {CustomerId}",
                        dto!.CustomerId);

                    return ValidationResult.Failure(CreditCardError.FailedGenerateCardNumber);
                }

                var assignedAt = DateTimeOffset.UtcNow;
                var creditCard = _mapper.Map<CreditCard>(dto!);
                creditCard.CardNumber = cardNumber;

                //Copia desnormalizada usada en listados, correos y logs: el número completo
                //nunca se expone. La unicidad del número ya la garantiza el generador.
                creditCard.LastFourDigits = cardNumber[^DomainConstants.LastFourDigitsLength..];

                //El CVC en claro solo existe dentro del hasher: aquí ya llega convertido
                creditCard.CvcHash = _cvcHasher.Hash(_cvcHasher.GenerateCvc());
                creditCard.ExpirationDate = assignedAt.AddYears(DomainConstants.CardExpirationYears);
                creditCard.AssignedByAdminId = adminUserId;
                creditCard.CreatedAt = assignedAt;
                creditCard.CreateByUserId = adminUserId;

                await _creditCardsRepository.AddAsync(creditCard);
                var result = await _creditCardsRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("La tarjeta de credito terminada {LastFourDigits} en el intento de " +
                        " asignación al cliente {CustomerId}, fallo en su asignación", creditCard.LastFourDigits, creditCard.CustomerId);
                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }
                _logger.LogInformation("Tarjeta de crédito terminada en {LastFourDigits} asignada al cliente {CustomerId}",
                    creditCard.LastFourDigits, creditCard.CustomerId);

                //Fuera de la transacción: un fallo de correo no revierte la tarjeta creada
                await SendCreditCardAssignedNotificationAsync(new CreditCardAssignedDto
                {
                    CustomerId = creditCard.CustomerId,
                    LastFourDigits = creditCard.LastFourDigits,
                    CreditLimit = creditCard.CreditLimit,
                    ExpirationDate = creditCard.ExpirationDate.ToString(ExpirationDateFormat),
                    AssignedAt = assignedAt
                });

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar la tarjeta de crédito al cliente {CustomerId}", dto!.CustomerId);
                return ValidationResult.Failure(CreditCardError.FailedProcessCreditCard);
            }
        }

        public async Task<ValidationResult<CardLimitUpdatedDto>> EditCreditCardLimitAsync(
            EditCardLimitDto dto)
        {
            _logger.LogInformation("Inicio de la modificación del límite de la tarjeta con ID {CreditCardId}", dto?.Id);

            var adminValidation = _creditCardsValidationServices.ValidateAdministratorInSession();
            if (!adminValidation.IsValid)
            {
                return ValidationResult<CardLimitUpdatedDto>.Failure(adminValidation.Errors.ToList());
            }

            var adminUserId = adminValidation.Value!;

            var validation = await _creditCardsValidationServices.ValidateLimitEditionAsync(dto!);
            if (!validation.IsValid)
            {
                return ValidationResult<CardLimitUpdatedDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var creditCard = validation.Value!;
                var modifiedAt = DateTimeOffset.UtcNow;

                creditCard.CreditLimit = dto!.CreditLimit;
                creditCard.ModifiedAt = modifiedAt;
                creditCard.LastModifiedByIdUser = adminUserId;

                await _creditCardsRepository.UpdateAsync(creditCard);
                var result = await _creditCardsRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("La modificación del límite de la tarjeta terminada en {LastFourDigits} no pudo confirmarse",
                        creditCard.LastFourDigits);

                    return ValidationResult<CardLimitUpdatedDto>.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Límite actualizado para la tarjeta terminada en {LastFourDigits}",
                    creditCard.LastFourDigits);

                var limitUpdated = new CardLimitUpdatedDto
                {
                    CustomerId = creditCard.CustomerId,
                    LastFourDigits = creditCard.LastFourDigits,
                    CreditLimit = creditCard.CreditLimit,
                    ModifiedAt = modifiedAt
                };

                //Fuera de la transacción: un fallo de correo no revierte el nuevo límite
                await SendCardLimitUpdatedNotificationAsync(limitUpdated);

                return ValidationResult<CardLimitUpdatedDto>.Success(limitUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar el límite de la tarjeta con ID {CreditCardId}", dto!.Id);
                return ValidationResult<CardLimitUpdatedDto>.Failure(CreditCardError.FailedProcessCreditCard);
            }
        }

        public async Task<ValidationResult> CancelCreditCardAsync(int creditCardId)
        {
            _logger.LogInformation("Inicio de la cancelación de la tarjeta con ID {CreditCardId}", creditCardId);

            var adminValidation = _creditCardsValidationServices.ValidateAdministratorInSession();
            if (!adminValidation.IsValid)
            {
                return ValidationResult.Failure(adminValidation.Errors.ToList());
            }

            var adminUserId = adminValidation.Value!;

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
                var result = await _creditCardsRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("La cancelación de la tarjeta terminada en {LastFourDigits} no pudo confirmarse",
                        creditCard.LastFourDigits);

                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

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
        #endregion

        #region notificaciones
        //El envío siempre ocurre fuera de la transacción: un fallo de correo no revierte la
        //asignación ni la modificación del límite, solo se informa como advertencia en el log.
        //Ningún correo lleva el número completo ni el CVC: solo los últimos 4 dígitos.
        private async Task<ValidationResult> SendCreditCardAssignedNotificationAsync(
            CreditCardAssignedDto assigned)
        {
            try
            {
                var customer = await _userManagementService.GetUserByIdAsync(assigned.CustomerId);

                if (customer is null)
                {
                    _logger.LogWarning("Sin datos de contacto del cliente {CustomerId}: no se envía el correo de asignación",
                        assigned.CustomerId);

                    return ValidationResult.Failure(CreditCardError.CreditCardCreatedWithoutNotification);
                }

                var message = new MessageDto
                {
                    To = customer.Email,
                    Subject = "Nueva tarjeta de crédito asignada",
                    Message = BuildCreditCardAssignedBody(assigned, $"{customer.Name} {customer.LastName}".Trim())
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar el correo de asignación de la tarjeta terminada en {LastFourDigits}. La operación no se revierte",
                    assigned.LastFourDigits);

                return ValidationResult.Failure(CreditCardError.CreditCardCreatedWithoutNotification);
            }
        }

        private async Task<ValidationResult> SendCardLimitUpdatedNotificationAsync(
            CardLimitUpdatedDto limitUpdated)
        {
            try
            {
                var customer = await _userManagementService.GetUserByIdAsync(limitUpdated.CustomerId);

                if (customer is null)
                {
                    _logger.LogWarning("Sin datos de contacto del cliente {CustomerId}: no se envía el correo de modificación de límite",
                        limitUpdated.CustomerId);

                    return ValidationResult.Failure(CreditCardError.CreditLimitUpdatedWithoutNotification);
                }

                var message = new MessageDto
                {
                    To = customer.Email,
                    Subject = "Modificación de límite de tarjeta",
                    Message = BuildCardLimitUpdatedBody(limitUpdated, $"{customer.Name} {customer.LastName}".Trim())
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar el correo de modificación de límite de la tarjeta terminada en {LastFourDigits}. La operación no se revierte",
                    limitUpdated.LastFourDigits);

                return ValidationResult.Failure(CreditCardError.CreditLimitUpdatedWithoutNotification);
            }
        }
        #endregion

        #region private methods
        //Una sola consulta a Identity por cliente distinto de la página
        private async Task FillCustomerDataAsync(IReadOnlyCollection<CreditCardDto> creditCards)
        {
            if (creditCards.Count == 0) return;

            var customers = new Dictionary<string, UserDetailDto?>();

            foreach (var creditCard in creditCards)
            {
                if (!customers.TryGetValue(creditCard.CustomerId, out var customer))
                {
                    customer = await _userManagementService.GetUserByIdAsync(creditCard.CustomerId);
                    customers[creditCard.CustomerId] = customer;
                }

                creditCard.FullNameCustomer = customer is null
                    ? string.Empty
                    : $"{customer.Name} {customer.LastName}".Trim();
            }
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
        #endregion

    }
}
