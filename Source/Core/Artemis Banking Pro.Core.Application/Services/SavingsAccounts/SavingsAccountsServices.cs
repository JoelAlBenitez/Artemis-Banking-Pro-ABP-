using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using Artemis_Banking_Pro.Core.Application.Services.Generic;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.CodeErrors.SavingsAccountsErrors;
using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.SavingsAccounts
{
    public sealed class SavingsAccountsServices :
        GenericServices<SavingsAccountAssignmentDto, SavingsAccountDto, int, SavingsAccount>,
        ISavingsAccountsServices
    {
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly ICreditCardsRepository _creditCardsRepository;
        private readonly ISavingsAccountsValidateServices _savingsAccountsValidateServices;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<SavingsAccountsServices> _logger;

        public SavingsAccountsServices(
            ISavingsAccountsRepository savingsAccountsRepository,
            ITransactionRepository transactionRepository,
            ILoansRepository loansRepository,
            ICreditCardsRepository creditCardsRepository,
            ISavingsAccountsValidateServices savingsAccountsValidateServices,
            IEmailServices emailServices,
            IMapper mapper,
            ILogger<SavingsAccountsServices> logger)
            : base(savingsAccountsRepository, mapper, logger)
        {
            _savingsAccountsRepository = savingsAccountsRepository;
            _transactionRepository = transactionRepository;
            _loansRepository = loansRepository;
            _creditCardsRepository = creditCardsRepository;
            _savingsAccountsValidateServices = savingsAccountsValidateServices;
            _emailServices = emailServices;
            _logger = logger;
        }

        #region query methods
        public async Task<ValidationResult<PagedResult<SavingsAccountDto>>> GetPagedSavingsAccountsAsync(
            SavingsAccountFilterDto filter, string? customerId)
        {
            try
            {
                _logger.LogInformation("Recuperando el listado de cuentas de ahorro. Página {Pagina}, estado {Estado}, tipo {Tipo}",
                    filter.Page, filter.Status, filter.Type);

                var queryValidation = await _savingsAccountsValidateServices.ValidateCustomerAccountsQueryAsync(filter);
                if (!queryValidation.IsValid)
                {
                    return ValidationResult<PagedResult<SavingsAccountDto>>.Failure(queryValidation.Errors.ToList());
                }

                var result = await _savingsAccountsRepository.GetPagedSavingsAccountsAsync(
                    filter.Page,
                    DomainConstants.DefaultPageSize,
                    ToSavingsAccountStatus(filter.Status),
                    ToSavingsAccountType(filter.Type),
                    customerId);

                if (!string.IsNullOrWhiteSpace(customerId) && result.TotalRecords == 0)
                {
                    _logger.LogWarning("El cliente consultado no tiene cuentas de ahorro registradas");
                    return ValidationResult<PagedResult<SavingsAccountDto>>.Failure(SavingsAccountError.NonExistsSavingsAccounts);
                }

                //El nombre y la cédula del cliente de cada cuenta provienen del project Identity y
                //se completan cuando su servicio de consulta de usuarios esté disponible.

                var items = _mapper.Map<IReadOnlyCollection<SavingsAccountDto>>(result.Items);
                var paged = new PagedResult<SavingsAccountDto>(
                    items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<SavingsAccountDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar el listado de cuentas de ahorro");
                return ValidationResult<PagedResult<SavingsAccountDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<PagedResult<TransactionDto>>> GetPagedTransactionsAsync(
            int savingsAccountId, int page)
        {
            try
            {
                _logger.LogInformation("Recuperando las transacciones de la cuenta de ahorro con ID {SavingsAccountId}. Página {Pagina}",
                    savingsAccountId, page);

                var savingsAccount = await _savingsAccountsRepository.GetByIdAsync(savingsAccountId);
                if (savingsAccount is null)
                {
                    _logger.LogWarning("Cuenta de ahorro con ID {SavingsAccountId} inexistente", savingsAccountId);
                    return ValidationResult<PagedResult<TransactionDto>>.Failure(SavingsAccountError.NonExistsSavingsAccount);
                }

                //Historial paginado del movimiento más reciente al más antiguo. La entidad
                //Transaction pertenece al módulo Cliente: este módulo solo la consulta.
                var result = await _transactionRepository.GetAllAsync(
                    page,
                    DomainConstants.DefaultPageSize,
                    transaction => transaction.SavingsAccountId == savingsAccountId,
                    query => query.OrderByDescending(transaction => transaction.CreatedAt));

                var items = _mapper.Map<IReadOnlyCollection<TransactionDto>>(result.Items);
                var paged = new PagedResult<TransactionDto>(
                    items, result.Page, result.PageSize, result.TotalRecords);

                return ValidationResult<PagedResult<TransactionDto>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar las transacciones de la cuenta de ahorro con ID {SavingsAccountId}",
                    savingsAccountId);

                return ValidationResult<PagedResult<TransactionDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<bool> IsAccountActiveAsync(string accountNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    return false;
                }

                var isActive = await _savingsAccountsRepository.ExistElementByConsult(
                    account => account.AccountNumber == accountNumber
                        && account.Status == SavingsAccountStatus.Activa);

                if (!isActive)
                {
                    _logger.LogWarning("La cuenta {AccountNumber} no existe o no está activa", accountNumber);
                }

                return isActive;
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al verificar el estado de la cuenta {AccountNumber}", accountNumber);
                return false;
            }
        }

        public async Task<decimal> GetCustomerTotalDebtAmountAsync(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return 0m;
                }

             
                var loansDebt = await _loansRepository.SumAsync(
                    loan => loan.CustomerId == customerId && loan.Status == LoanStatus.Activo,
                    loan => loan.PendingAmount);

                var cardsDebt = await _creditCardsRepository.SumAsync(
                    card => card.CustomerId == customerId && card.Status == CreditCardStatus.Activa,
                    card => card.OwedAmount);

                return loansDebt + cardsDebt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular la deuda total del cliente {CustomerId}", customerId);
                return 0m;
            }
        }

        //Paso 1 de la asignación: listado de clientes activos con su deuda total. La deuda ya
        //está resuelta arriba; falta el project Identity para obtener los clientes activos con
        //su cédula, nombre y correo. Descomentar cuando IUserServices exista.

        //public async Task<ValidationResult<IReadOnlyCollection<ClientSavingsAccountDto>>> GetActiveClientsAsync()
        //{
        //    var customers = await _userServices.GetActiveCustomersAsync();
        //    var clients = new List<ClientSavingsAccountDto>();

        //    foreach (var customer in customers)
        //    {
        //        clients.Add(new ClientSavingsAccountDto
        //        {
        //            Id = customer.Id,
        //            IdCard = customer.IdCard,
        //            FullName = customer.FullName,
        //            Email = customer.Email,
        //            TotalDebtAmount = await GetCustomerTotalDebtAmountAsync(customer.Id)
        //        });
        //    }

        //    return ValidationResult<IReadOnlyCollection<ClientSavingsAccountDto>>.Success(clients);
        //}
        #endregion

        #region write methods
        public async Task<ValidationResult> AssignSavingsAccountAsync(SavingsAccountAssignmentDto dto)
        {
            _logger.LogInformation("Inicio de la asignación de una cuenta de ahorro secundaria al cliente {CustomerId}",
                dto.CustomerId);

            //agregar validacion del admin User Id -> falta metodo de adrian

            var validation = await _savingsAccountsValidateServices.ValidateAssignmentAsync(dto);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                //Número de 9 dígitos emitido por SavingsAccountNumberSequence. Su rango es
                //disjunto del de los préstamos, así que la unicidad entre ambos productos está
                //garantizada por la base de datos sin consultar registros.
                var accountNumber = await _savingsAccountsRepository.GetNextAccountNumberAsync();
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    _logger.LogError("No fue posible generar un número de cuenta para el cliente {CustomerId}",
                        dto.CustomerId);

                    return ValidationResult.Failure(SavingsAccountError.FailedGenerateAccountNumber);
                }

                var assignedAt = DateTimeOffset.UtcNow;
                var savingsAccount = _mapper.Map<SavingsAccount>(dto);
                savingsAccount.AccountNumber = accountNumber;
                savingsAccount.CreatedAt = assignedAt;
                savingsAccount.CreateByUserId = ""; // por modificar

                await _savingsAccountsRepository.AddAsync(savingsAccount);

                //Un único SaveChangesAsync confirma la cuenta y su transacción inicial dentro de
                //la misma transacción de EF Core: la operación es atómica.
                if (dto.InitialBalance > 0m)
                {
                    //Todo balance inicial mayor que cero se registra como una transacción de tipo
                    //CRÉDITO en el historial de la cuenta.
                    //El módulo Cliente expone ITransactionService.RegisterInitialTransactionAsync,
                    //pero su DTO solo lleva el SavingsAccountId, que no existe antes de persistir:
                    //llamarlo obligaría a un segundo SaveChangesAsync y rompería el todo o nada.
                    //Se escribe con el repositorio para que EF resuelva la FK por navegación.
                    await _transactionRepository.AddAsync(new Transaction
                    {
                        SavingsAccount = savingsAccount,
                        SavingsAccountId = default,
                        Amount = dto.InitialBalance,
                        TransactionType = TransactionType.Credito,
                        OperationType = OperationType.AperturaCuenta,
                        Origin = accountNumber,
                        Beneficiary = accountNumber,
                        Status = TransactionStatus.Aprobada,
                        Channel = ChannelPayment.Administrador,
                        //Hasta que exista ICurrentUserServices no hay administrador autenticado
                        //que atribuir: la auditoría queda a nombre del sistema.
                        PerformedByUserId = DomainConstants.SystemUserId,
                        CreatedAt = assignedAt,
                        CreateByUserId = DomainConstants.SystemUserId
                    });

                    _logger.LogInformation("La cuenta {AccountNumber} se crea con un balance inicial de RD${Balance} registrado como CRÉDITO",
                        accountNumber, dto.InitialBalance);
                }

                var result = await _savingsAccountsRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("La cuenta de ahorro {AccountNumber} en el intento de asignación al cliente {CustomerId}, falló en su asignación",
                        accountNumber, dto.CustomerId);

                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Cuenta de ahorro secundaria {AccountNumber} asignada al cliente {CustomerId}",
                    accountNumber, dto.CustomerId);

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar la cuenta de ahorro al cliente {CustomerId}", dto.CustomerId);
                return ValidationResult.Failure(SavingsAccountError.FailedProcessSavingsAccount);
            }
        }

        public async Task<ValidationResult> CancelSavingsAccountAsync(int savingsAccountId)
        {
            _logger.LogInformation("Inicio de la cancelación de la cuenta de ahorro con ID {SavingsAccountId}",
                savingsAccountId);

            var validation = await _savingsAccountsValidateServices.ValidateCancellationAsync(savingsAccountId);
            if (!validation.IsValid)
            {
                return ValidationResult.Failure(validation.Errors.ToList());
            }

            try
            {
                var savingsAccount = validation.Value!;
                var cancelledAt = DateTimeOffset.UtcNow;
                var transferredAmount = savingsAccount.Balance;

                //Receptora del balance remanente. UpdateAsync la adjunta al contexto, así que no
                //necesita venir rastreada de la consulta.
                var primaryAccount = await _savingsAccountsRepository.GetFirstAsync(
                    account => account.CustomerId == savingsAccount.CustomerId
                        && account.AccountType == SavingsAccountType.Principal
                        && account.Status == SavingsAccountStatus.Activa);

                if (primaryAccount is null)
                {
                    _logger.LogWarning("Cancelación abortada: el cliente {CustomerId} dejó de tener una cuenta principal activa receptora",
                        savingsAccount.CustomerId);

                    return ValidationResult.Failure(SavingsAccountError.WithoutPrimaryAccountToReceiveFunds);
                }

                //La transferencia ocurre antes de cambiar el estado de la cuenta secundaria
                if (transferredAmount > 0m)
                {
                    savingsAccount.Balance = 0m;
                    primaryAccount.Balance += transferredAmount;
                    primaryAccount.ModifiedAt = cancelledAt;
                    primaryAccount.LastModifiedByIdUser = ""; // por modificar

                    await _savingsAccountsRepository.UpdateAsync(primaryAccount);

                    //Se registran dos transacciones cruzadas: un DÉBITO en la secundaria
                    //cancelada y un CRÉDITO en la principal receptora. Ambas comparten Origen
                    //(la secundaria) y Beneficiario (la principal): esos campos describen el
                    //movimiento, no el lado desde el que se mira el asiento.
                    var debitEntry = new Transaction
                    {
                        SavingsAccountId = savingsAccount.Id,
                        Amount = transferredAmount,
                        TransactionType = TransactionType.Debito,
                        OperationType = OperationType.CancelacionCuenta,
                        Origin = savingsAccount.AccountNumber,
                        Beneficiary = primaryAccount.AccountNumber,
                        Status = TransactionStatus.Aprobada,
                        Channel = ChannelPayment.Administrador,
                        PerformedByUserId = DomainConstants.SystemUserId,
                        CreatedAt = cancelledAt,
                        CreateByUserId = DomainConstants.SystemUserId
                    };

                    //El Id del débito no existe antes de SaveChangesAsync: el enlace se declara
                    //por navegación y EF resuelve la FK al persistir ambos asientos.
                    var creditEntry = new Transaction
                    {
                        SavingsAccountId = primaryAccount.Id,
                        Amount = transferredAmount,
                        TransactionType = TransactionType.Credito,
                        OperationType = OperationType.CancelacionCuenta,
                        Origin = savingsAccount.AccountNumber,
                        Beneficiary = primaryAccount.AccountNumber,
                        Status = TransactionStatus.Aprobada,
                        Channel = ChannelPayment.Administrador,
                        PerformedByUserId = DomainConstants.SystemUserId,
                        RelatedTransaction = debitEntry,
                        CreatedAt = cancelledAt,
                        CreateByUserId = DomainConstants.SystemUserId
                    };

                    await _transactionRepository.AddAsync(debitEntry);
                    await _transactionRepository.AddAsync(creditEntry);

                    _logger.LogInformation("Balance de RD${Monto} transferido de la cuenta {AccountNumber} a la principal {PrimaryAccountNumber}",
                        transferredAmount, savingsAccount.AccountNumber, primaryAccount.AccountNumber);
                }

                savingsAccount.Status = SavingsAccountStatus.Cancelada;
                savingsAccount.StatusChangedAt = cancelledAt;
                savingsAccount.ModifiedAt = cancelledAt;
                savingsAccount.LastModifiedByIdUser = ""; // por modificar

                await _savingsAccountsRepository.UpdateAsync(savingsAccount);

                //Un único SaveChangesAsync confirma ambos balances y el cambio de estado:
                //todo o nada. La cuenta no se elimina físicamente ni pierde su historial.
                var result = await _savingsAccountsRepository.SaveChangesAsync();
                if (result <= 0)
                {
                    _logger.LogWarning("La cancelación de la cuenta de ahorro {AccountNumber} no pudo confirmarse",
                        savingsAccount.AccountNumber);

                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Cuenta de ahorro {AccountNumber} cancelada. El historial de transacciones se conserva",
                    savingsAccount.AccountNumber);

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar la cuenta de ahorro con ID {SavingsAccountId}", savingsAccountId);
                return ValidationResult.Failure(SavingsAccountError.FailedProcessSavingsAccount);
            }
        }
        #endregion

        //modificar cuando se integre el ICurrentUserServices para obtener el correo y el nombre
        //del cliente. El envío siempre ocurre fuera de la transacción: un fallo de correo no
        //revierte la asignación ni la cancelación, solo se informa como advertencia.

        //public async Task<ValidationResult> SendSavingsAccountAssignedNotificationAsync(
        //    SavingsAccountAssignedDto assigned, string customerEmail, string customerFullName)
        //{
        //    var message = new MessageDto
        //    {
        //        To = customerEmail,
        //        Subject = "Nueva cuenta de ahorro asignada",
        //        Message = BuildSavingsAccountAssignedBody(assigned, customerFullName)
        //    };

        //    var sent = await _emailServices.SendNotification(message);

        //    if (!sent)
        //    {
        //        _logger.LogWarning("No fue posible enviar el correo de asignación de la cuenta {AccountNumber}. La operación no se revierte",
        //            assigned.AccountNumber);

        //        return ValidationResult.Failure(SavingsAccountError.SavingsAccountCreatedWithoutNotification);
        //    }

        //    return ValidationResult.Success();
        //}

        //public async Task<ValidationResult> SendSavingsAccountCancelledNotificationAsync(
        //    SavingsAccountCancelledDto cancelled, string customerEmail, string customerFullName)
        //{
        //    var message = new MessageDto
        //    {
        //        To = customerEmail,
        //        Subject = "Cancelación de cuenta de ahorro",
        //        Message = BuildSavingsAccountCancelledBody(cancelled, customerFullName)
        //    };

        //    var sent = await _emailServices.SendNotification(message);

        //    if (!sent)
        //    {
        //        _logger.LogWarning("No fue posible enviar el correo de cancelación de la cuenta {AccountNumber}. La operación no se revierte",
        //            cancelled.AccountNumber);

        //        return ValidationResult.Failure(SavingsAccountError.SavingsAccountCancelledWithoutNotification);
        //    }

        //    return ValidationResult.Success();
        //}

        #region private methods
        //private static string BuildSavingsAccountAssignedBody(
        //    SavingsAccountAssignedDto assigned, string customerFullName)
        //    => $"<p>Hola {customerFullName},</p>" +
        //       "<p>Se ha asignado una nueva cuenta de ahorro secundaria a su perfil.</p>" +
        //       $"<p>Número de cuenta: {assigned.AccountNumber}<br/>" +
        //       $"Balance inicial: RD${assigned.InitialBalance:N2}<br/>" +
        //       $"Fecha de asignación: {assigned.AssignedAt:dd/MM/yyyy}</p>" +
        //       "<p>Si usted no reconoce esta asignación, comuníquese con la entidad bancaria.</p>";

        //private static string BuildSavingsAccountCancelledBody(
        //    SavingsAccountCancelledDto cancelled, string customerFullName)
        //    => $"<p>Hola {customerFullName},</p>" +
        //       $"<p>Su cuenta de ahorro {cancelled.AccountNumber} ha sido cancelada.</p>" +
        //       $"<p>Monto transferido a su cuenta principal: RD${cancelled.TransferredAmount:N2}<br/>" +
        //       $"Cuenta principal receptora: {cancelled.PrimaryAccountNumber}<br/>" +
        //       $"Fecha de cancelación: {cancelled.CancelledAt:dd/MM/yyyy}</p>" +
        //       "<p>Si usted no reconoce esta cancelación, comuníquese con la entidad bancaria.</p>";

        private static SavingsAccountStatus? ToSavingsAccountStatus(SavingsAccountStatusFilter filter)
            => filter switch
            {
                SavingsAccountStatusFilter.Activas => SavingsAccountStatus.Activa,
                SavingsAccountStatusFilter.Canceladas => SavingsAccountStatus.Cancelada,
                _ => null
            };

        private static SavingsAccountType? ToSavingsAccountType(SavingsAccountTypeFilter filter)
            => filter switch
            {
                SavingsAccountTypeFilter.Principal => SavingsAccountType.Principal,
                SavingsAccountTypeFilter.Secundaria => SavingsAccountType.Secundaria,
                _ => null
            };
        #endregion
    }
}
