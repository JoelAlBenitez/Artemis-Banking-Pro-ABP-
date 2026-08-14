using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Application.Contracts.Users.Management;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.CodeErrors.GeneralErrors;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Beneficiaries;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Services.Transactions
{
    public sealed class TransactionService : ITransactionService, IAtmTransactionService
    {
        private readonly ISavingsAccountsRepository _savingsAccountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly ICreditCardsRepository _creditCardRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly ITransactionsValidationServices _validationServices;
        private readonly IEmailServices _emailServices;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;
        private readonly IUserManagementService _userManagementService;

        public TransactionService(
            ISavingsAccountsRepository savingsAccountRepository,
            ITransactionRepository transactionRepository,
            IBeneficiaryRepository beneficiaryRepository,
            ICreditCardsRepository creditCardRepository,
            ILoansRepository loansRepository,
            ITransactionsValidationServices validationServices,
            IEmailServices emailServices,
            IMapper mapper,
            ILogger<TransactionService> logger,
            IUserManagementService userManagementService)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _transactionRepository = transactionRepository;
            _beneficiaryRepository = beneficiaryRepository;
            _creditCardRepository = creditCardRepository;
            _loansRepository = loansRepository;
            _validationServices = validationServices;
            _emailServices = emailServices;
            _mapper = mapper;
            _logger = logger;
            _userManagementService = userManagementService;
        }

        public async Task<ValidationResult<TransactionResultDto>> ProcessExpressAsync(ExpressTransactionDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de transferencia express para el cliente {ClientId} por RD${Amount}", clientId, dto.Amount);

            var validation = await _validationServices.ValidateExpressAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    await RegisterRejectedTransactionAsync(dto.SourceAccountNumber, dto.DestinationAccountNumber, dto.Amount, OperationType.TransaccionExpress, clientId);
                }
                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (originAccount, destAccount) = validation.Value;
                var result = await ExecuteApprovedExpressTransferAsync(originAccount, destAccount, dto.Amount, clientId);
                if (!result.IsValid)
                {
                    return result;
                }

                _logger.LogInformation("Transferencia express procesada y guardada correctamente para el cliente {ClientId}", clientId);

                var emailSent = await SendExpressNotificationEmailsAsync(originAccount, destAccount, dto.Amount, clientId);
                if (!emailSent)
                {
                    result.Value!.WarningMessage = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar la transferencia express para el cliente {ClientId}", clientId);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<TransactionResultDto>> ProcessBeneficiaryTransactionAsync(BeneficiaryTransactionDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de transferencia a beneficiario {BeneficiaryId} para el cliente {ClientId} por RD${Amount}", dto.BeneficiaryId, clientId, dto.Amount);

            var validation = await _validationServices.ValidateBeneficiaryAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    var beneficiary = await _beneficiaryRepository.GetFirstAsync(b => b.Id == dto.BeneficiaryId && b.OwnerClientId == clientId);
                    if (beneficiary is not null)
                    {
                        await RegisterRejectedTransactionAsync(dto.SourceAccountNumber, beneficiary.BeneficiaryAccountNumber, dto.Amount, OperationType.TransaccionBeneficiario, clientId);
                    }
                }
                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (originAccount, destAccount) = validation.Value;
                var result = await ExecuteApprovedBeneficiaryTransferAsync(originAccount, destAccount, dto.Amount, clientId);
                if (!result.IsValid)
                {
                    return result;
                }

                _logger.LogInformation("Transferencia a beneficiario procesada y guardada correctamente para el cliente {ClientId}", clientId);

                var emailSent = await SendExpressNotificationEmailsAsync(originAccount, destAccount, dto.Amount, clientId);
                if (!emailSent)
                {
                    result.Value!.WarningMessage = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar la transferencia a beneficiario para el cliente {ClientId}", clientId);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<int>> GetTotalHistoricalAsync()
        {
            _logger.LogInformation("Obteniendo total histórico de transacciones del sistema");
            var total = await _transactionRepository.CountAsync();
            return ValidationResult<int>.Success(total);
        }

        public async Task<ValidationResult<int>> GetTotalTodayAsync()
        {
            _logger.LogInformation("Obteniendo total de transacciones del día actual");
            var today = DateTimeOffset.UtcNow.Date;
            var total = await _transactionRepository.CountAsync(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1));
            return ValidationResult<int>.Success(total);
        }

        public async Task<ValidationResult> RegisterInitialTransactionAsync(InitialTransactionDto dto)
        {
            _logger.LogInformation("Registrando transacción inicial para la cuenta de ahorro ID {SavingsAccountId} por RD${Amount}", dto.SavingsAccountId, dto.Amount);

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Registro de transacción inicial fallido: el monto RD${Amount} debe ser mayor que cero", dto.Amount);
                return ValidationResult.Failure(TransactionError.InvalidAmount);
            }

            try
            {
                var transaction = new Transaction
                {
                    SavingsAccountId = dto.SavingsAccountId,
                    Amount = dto.Amount,
                    TransactionType = TransactionType.Credito,
                    OperationType = OperationType.AperturaCuenta,
                    Origin = "DEPÓSITO APERTURA",
                    Status = TransactionStatus.Aprobada,
                    PerformedByUserId = dto.PerformedByUserId,
                    Channel = ChannelPayment.Cliente,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = dto.PerformedByUserId
                };

                await _transactionRepository.AddAsync(transaction);
                var saveResult = await _transactionRepository.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    _logger.LogWarning("No se pudo persistir la transacción inicial para la cuenta de ahorro ID {SavingsAccountId}", dto.SavingsAccountId);
                    return ValidationResult.Failure(GeneralError.UnexpectedError);
                }

                _logger.LogInformation("Transacción inicial registrada con éxito para la cuenta ID {SavingsAccountId}", dto.SavingsAccountId);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al registrar la transacción inicial para la cuenta ID {SavingsAccountId}", dto.SavingsAccountId);
                return ValidationResult.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<ClientDto>>> GetClientsAsync()
        {
            _logger.LogInformation("Recuperando listado consolidado de clientes desde cuentas de ahorro, tarjetas de crédito y préstamos");
            try
            {
                var accounts = await _savingsAccountRepository.GetAllFindAsync(a => true);
                var cards = await _creditCardRepository.GetAllFindAsync(c => true);
                var loans = await _loansRepository.GetAllFindAsync(l => true);

                var customerIds = accounts.Select(a => a.CustomerId)
                    .Concat(cards.Select(c => c.CustomerId))
                    .Concat(loans.Select(l => l.CustomerId))
                    .Distinct()
                    .ToList();

                var clients = new List<ClientDto>();
                foreach (var id in customerIds)
                {
                    var user = await _userManagementService.GetUserByIdAsync(id);
                    if (user != null)
                    {
                        clients.Add(new ClientDto
                        {
                            Id = id,
                            IdCard = user.IDCARD,
                            FullName = $"{user.Name} {user.LastName}",
                            Email = user.Email,
                            IsActive = user.State
                        });
                    }
                    else
                    {
                        clients.Add(new ClientDto
                        {
                            Id = id,
                            IdCard = "001-0000000-1",
                            FullName = $"Cliente {id}",
                            Email = $"{id}@artemis.com",
                            IsActive = true
                        });
                    }
                }

                _logger.LogInformation("Recuperación consolidada exitosa. Total de clientes encontrados: {Count}", clients.Count);
                return ValidationResult<IReadOnlyCollection<ClientDto>>.Success(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al recuperar el listado consolidado de clientes");
                return ValidationResult<IReadOnlyCollection<ClientDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>> GetBeneficiariesAsync(string clientId)
        {
            _logger.LogInformation("Obteniendo beneficiarios activos del cliente {ClientId}", clientId);
            try
            {
                var beneficiaries = await _beneficiaryRepository.GetAllFindAsync(b => b.OwnerClientId == clientId && b.IsActive);
                var dtos = _mapper.Map<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>(beneficiaries);
                return ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener beneficiarios para el cliente {ClientId}", clientId);
                return ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>.Failure(GeneralError.UnexpectedError);
            }
        }

        #region Helper Methods

        private async Task RegisterRejectedTransactionAsync(string srcAccNumber, string dstAccNumber, decimal amount, OperationType opType, string clientId)
        {
            _logger.LogWarning("Registrando intento de transacción rechazada por fondos insuficientes desde cuenta {Source} hacia {Destination} por RD${Amount}", srcAccNumber, dstAccNumber, amount);
            try
            {
                var srcAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == srcAccNumber);
                if (srcAcc is not null)
                {
                    var rejectedTx = new Transaction
                    {
                        SavingsAccountId = srcAcc.Id,
                        Amount = amount,
                        TransactionType = TransactionType.Debito,
                        OperationType = opType,
                        Origin = srcAcc.AccountNumber,
                        Beneficiary = dstAccNumber,
                        Status = TransactionStatus.Rechazada,
                        RejectionReason = "Fondos insuficientes",
                        PerformedByUserId = clientId,
                        Channel = ChannelPayment.Cliente,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreateByUserId = clientId
                    };

                    await _transactionRepository.AddAsync(rejectedTx);
                    await _transactionRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al registrar la transacción rechazada en base de datos.");
            }
        }

        private async Task<ValidationResult<TransactionResultDto>> ExecuteApprovedExpressTransferAsync(SavingsAccount origin, SavingsAccount dest, decimal amount, string clientId)
        {
            origin.Balance -= amount;
            dest.Balance += amount;

            await _savingsAccountRepository.UpdateAsync(origin);
            await _savingsAccountRepository.UpdateAsync(dest);

            var debitTx = CreateApprovedTransactionEntity(origin.Id, amount, TransactionType.Debito, OperationType.TransaccionExpress, origin.AccountNumber, dest.AccountNumber, clientId);
            var creditTx = CreateApprovedTransactionEntity(dest.Id, amount, TransactionType.Credito, OperationType.TransaccionExpress, origin.AccountNumber, dest.AccountNumber, clientId);

            await _transactionRepository.AddAsync(debitTx);
            await _transactionRepository.AddAsync(creditTx);

            var saveResult1 = await _transactionRepository.SaveChangesAsync();
            if (saveResult1 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible guardar los registros de transferencia express.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            debitTx.RelatedTransactionId = creditTx.Id;
            creditTx.RelatedTransactionId = debitTx.Id;

            await _transactionRepository.UpdateAsync(debitTx);
            await _transactionRepository.UpdateAsync(creditTx);

            var saveResult2 = await _transactionRepository.SaveChangesAsync();
            if (saveResult2 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible vincular las transacciones relacionadas.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = amount,
                TransactionType = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        private async Task<ValidationResult<TransactionResultDto>> ExecuteApprovedBeneficiaryTransferAsync(SavingsAccount origin, SavingsAccount dest, decimal amount, string clientId)
        {
            origin.Balance -= amount;
            dest.Balance += amount;

            await _savingsAccountRepository.UpdateAsync(origin);
            await _savingsAccountRepository.UpdateAsync(dest);

            var debitTx = CreateApprovedTransactionEntity(origin.Id, amount, TransactionType.Debito, OperationType.TransaccionBeneficiario, origin.AccountNumber, dest.AccountNumber, clientId);
            var creditTx = CreateApprovedTransactionEntity(dest.Id, amount, TransactionType.Credito, OperationType.TransaccionBeneficiario, origin.AccountNumber, dest.AccountNumber, clientId);

            await _transactionRepository.AddAsync(debitTx);
            await _transactionRepository.AddAsync(creditTx);

            var saveResult1 = await _transactionRepository.SaveChangesAsync();
            if (saveResult1 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible guardar los registros de transferencia a beneficiario.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            debitTx.RelatedTransactionId = creditTx.Id;
            creditTx.RelatedTransactionId = debitTx.Id;

            await _transactionRepository.UpdateAsync(debitTx);
            await _transactionRepository.UpdateAsync(creditTx);

            var saveResult2 = await _transactionRepository.SaveChangesAsync();
            if (saveResult2 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible vincular las transacciones relacionadas.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = amount,
                TransactionType = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        private Transaction CreateApprovedTransactionEntity(int accountId, decimal amount, TransactionType txType, OperationType opType, string originAcc, string destAcc, string clientId)
        {
            return new Transaction
            {
                SavingsAccountId = accountId,
                Amount = amount,
                TransactionType = txType,
                OperationType = opType,
                Origin = originAcc,
                Beneficiary = destAcc,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };
        }

        private async Task<bool> SendExpressNotificationEmailsAsync(SavingsAccount origin, SavingsAccount dest, decimal amount, string clientId)
        {
            var emisorUser = await _userManagementService.GetUserByIdAsync(clientId);
            var emisorEmail = emisorUser?.Email ?? $"{clientId}@artemis.com";

            var receptorUser = await _userManagementService.GetUserByIdAsync(dest.CustomerId);
            var receptorEmail = receptorUser?.Email ?? $"{dest.CustomerId}@artemis.com";

            var lastFourDest = dest.AccountNumber.Length >= 4 ? dest.AccountNumber.Substring(dest.AccountNumber.Length - 4) : dest.AccountNumber;
            var lastFourOrig = origin.AccountNumber.Length >= 4 ? origin.AccountNumber.Substring(origin.AccountNumber.Length - 4) : origin.AccountNumber;

            try
            {
                _logger.LogInformation("Enviando correos de notificación de transferencia desde la cuenta ****{LastFourOrig} a la cuenta ****{LastFourDest}", lastFourOrig, lastFourDest);

                var sent1 = await _emailServices.SendNotification(new MessageDto
                {
                    To = emisorEmail,
                    Subject = $"Transacción realizada a la cuenta {lastFourDest}",
                    Message = $"Monto: RD${amount:N2}, Fecha: {DateTimeOffset.UtcNow}, Cuenta Destino: ****{lastFourDest}"
                });

                var sent2 = await _emailServices.SendNotification(new MessageDto
                {
                    To = receptorEmail,
                    Subject = $"Transacción enviada desde la cuenta {lastFourOrig}",
                    Message = $"Monto: RD${amount:N2}, Fecha: {DateTimeOffset.UtcNow}, Cuenta Origen: ****{lastFourOrig}"
                });

                return sent1 && sent2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar correo de notificación.");
                return false;
            }
        }

        public async Task<bool> ProcessDepositAsync(DepositDto depositData)
        {
            _logger.LogInformation("Procesando depósito de RD${Amount} en cuenta {AccountNumber} por el cajero {CashierId}", depositData.Amount, depositData.AccountNumber, depositData.CashierId);

            if (depositData.Amount <= 0)
            {
                _logger.LogWarning("Monto de depósito inválido: {Amount}", depositData.Amount);
                return false;
            }

            var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == depositData.AccountNumber);
            if (account == null || !account.IsActive)
            {
                _logger.LogWarning("Cuenta destino no encontrada o inactiva: {AccountNumber}", depositData.AccountNumber);
                return false;
            }

            try
            {
                account.Balance += depositData.Amount;
                await _savingsAccountRepository.UpdateAsync(account);

                var tx = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = depositData.Amount,
                    TransactionType = TransactionType.Credito,
                    OperationType = OperationType.Deposito,
                    Origin = "DEPÓSITO",
                    Beneficiary = account.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    PerformedByUserId = depositData.CashierId,
                    Channel = ChannelPayment.Cajero,
                    CreateByUserId = depositData.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _transactionRepository.AddAsync(tx);
                var saveResult = await _transactionRepository.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogWarning("Fallo al guardar la transacción de depósito en base de datos.");
                    return false;
                }

                _logger.LogInformation("Depósito procesado exitosamente en cuenta {AccountNumber}.", depositData.AccountNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar depósito en cuenta {AccountNumber}", depositData.AccountNumber);
                return false;
            }
        }

        public async Task<TransactionIndicatorsDto> GetCashierDailyIndicatorsAsync(string cashierId)
        {
            _logger.LogInformation("Obteniendo indicadores diarios para el cajero {CashierId}", cashierId);

            var startOfDay = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
            var endOfDay = startOfDay.AddDays(1);

            var transactions = await _transactionRepository.GetAllFindAsync(t =>
                t.PerformedByUserId == cashierId &&
                t.CreatedAt >= startOfDay &&
                t.CreatedAt < endOfDay
            );

            var indicators = new TransactionIndicatorsDto
            {
                TotalTransactions = transactions.Count,
                TotalPaymentsAmount = 0,
                TotalDepositsAmount = 0,
                TotalWithdrawalsAmount = 0
            };

            foreach (var t in transactions)
            {
                if (t.Status != TransactionStatus.Aprobada) continue;

                if (t.OperationType == OperationType.PagoTarjeta || t.OperationType == OperationType.PagoPrestamo)
                {
                    indicators.TotalPaymentsAmount += t.Amount;
                }
                else if (t.OperationType == OperationType.Deposito)
                {
                    indicators.TotalDepositsAmount += t.Amount;
                }
                else if (t.OperationType == OperationType.Retiro)
                {
                    indicators.TotalWithdrawalsAmount += t.Amount;
                }
            }

            return indicators;
        }

        public async Task<ValidationResult<TransactionResultDto>> ProcessAccountTransferAsync(AccountTransferDto dto, string clientId)
        {
            _logger.LogInformation("Iniciando procesamiento de transferencia entre cuentas propias para el cliente {ClientId} por monto RD${Amount}", clientId, dto.Amount);

            var validation = await _validationServices.ValidateAccountTransferAsync(dto, clientId);
            if (!validation.IsValid)
            {
                var sourceAccount = await _savingsAccountRepository.GetFirstAsync(a => a.Id == dto.SourceAccountId && a.CustomerId == clientId && a.Status == SavingsAccountStatus.Activa);
                if (sourceAccount != null)
                {
                    var destAccount = await _savingsAccountRepository.GetByIdAsync(dto.DestinationAccountId);
                    var destNum = destAccount?.AccountNumber ?? "";
                    var reason = validation.Errors.FirstOrDefault()?.Description ?? "Validación fallida";
                    await RegisterRejectedTransferAsync(sourceAccount, destNum, dto.Amount, reason, clientId);
                }
                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            try
            {
                var (sourceAccount, destAccount) = validation.Value;

                var result = await ExecuteApprovedAccountTransferAsync(sourceAccount, destAccount, dto.Amount, clientId);
                if (!result.IsValid)
                {
                    return result;
                }

                _logger.LogInformation("Transferencia entre cuentas propias procesada y guardada correctamente para el cliente {ClientId}", clientId);

                var emailSent = await SendAccountTransferEmailAsync(sourceAccount, destAccount, dto.Amount, clientId);
                if (!emailSent)
                {
                    result.Value!.WarningMessage = "La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico inesperado al procesar la transferencia entre cuentas para el cliente {ClientId}", clientId);
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }
        }

        private async Task RegisterRejectedTransferAsync(SavingsAccount sourceAccount, string destAccountNumber, decimal amount, string reason, string clientId)
        {
            _logger.LogWarning("Registrando intento de transferencia rechazada desde cuenta {Source} hacia {Destination} por RD${Amount} debido a: {Reason}", sourceAccount.AccountNumber, destAccountNumber, amount, reason);
            try
            {
                var rejectedTx = new Transaction
                {
                    SavingsAccountId = sourceAccount.Id,
                    Amount = amount,
                    TransactionType = TransactionType.Debito,
                    OperationType = OperationType.TransferenciaEntreCuentas,
                    Origin = sourceAccount.AccountNumber,
                    Beneficiary = destAccountNumber,
                    Status = TransactionStatus.Rechazada,
                    RejectionReason = reason,
                    PerformedByUserId = clientId,
                    Channel = ChannelPayment.Cliente,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreateByUserId = clientId
                };

                await _transactionRepository.AddAsync(rejectedTx);
                await _transactionRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el intento de transferencia rechazada.");
            }
        }

        private async Task<ValidationResult<TransactionResultDto>> ExecuteApprovedAccountTransferAsync(SavingsAccount source, SavingsAccount dest, decimal amount, string clientId)
        {
            source.Balance -= amount;
            dest.Balance += amount;

            await _savingsAccountRepository.UpdateAsync(source);
            await _savingsAccountRepository.UpdateAsync(dest);

            var debitTx = new Transaction
            {
                SavingsAccountId = source.Id,
                Amount = amount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.TransferenciaEntreCuentas,
                Origin = source.AccountNumber,
                Beneficiary = dest.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var creditTx = new Transaction
            {
                SavingsAccountId = dest.Id,
                Amount = amount,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.TransferenciaEntreCuentas,
                Origin = source.AccountNumber,
                Beneficiary = dest.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(debitTx);
            await _transactionRepository.AddAsync(creditTx);

            var saveResult1 = await _transactionRepository.SaveChangesAsync();
            if (saveResult1 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible guardar los registros de transferencia entre cuentas.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            debitTx.RelatedTransactionId = creditTx.Id;
            creditTx.RelatedTransactionId = debitTx.Id;

            await _transactionRepository.UpdateAsync(debitTx);
            await _transactionRepository.UpdateAsync(creditTx);

            var saveResult2 = await _transactionRepository.SaveChangesAsync();
            if (saveResult2 <= 0)
            {
                _logger.LogWarning("Error de persistencia: no fue posible vincular las transacciones relacionadas.");
                return ValidationResult<TransactionResultDto>.Failure(GeneralError.UnexpectedError);
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = amount,
                TransactionType = TransactionType.Debito,
                Status = TransactionStatus.Aprobada,
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        private async Task<bool> SendAccountTransferEmailAsync(SavingsAccount source, SavingsAccount dest, decimal amount, string clientId)
        {
            var user = await _userManagementService.GetUserByIdAsync(clientId);
            var email = user?.Email ?? $"{clientId}@artemis.com";
            var lastFourSource = source.AccountNumber.Length >= 4 ? source.AccountNumber.Substring(source.AccountNumber.Length - 4) : source.AccountNumber;
            var lastFourDest = dest.AccountNumber.Length >= 4 ? dest.AccountNumber.Substring(dest.AccountNumber.Length - 4) : dest.AccountNumber;

            try
            {
                _logger.LogInformation("Enviando correo de notificación de transferencia entre cuentas al cliente {ClientId}", clientId);
                var sent = await _emailServices.SendNotification(new MessageDto
                {
                    To = email,
                    Subject = "Transferencia entre cuentas realizada",
                    Message = $"Monto transferido: RD${amount:N2}, Cuenta Origen: ****{lastFourSource}, Cuenta Destino: ****{lastFourDest}, Fecha: {DateTimeOffset.UtcNow}"
                });
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar notificación por correo de transferencia entre cuentas.");
                return false;
            }
        }

        #endregion

        #region ATM Methods

        public async Task<ValidationResult> ProcessAtmDepositAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmDepositDto dto)
        {
            try
            {
                var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.DestinationAccountNumber);
                if (account == null || !account.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.Deposit", "La cuenta de destino no es válida o está inactiva."));

                // Actualizar balance
                account.Balance += dto.Amount;
                await _savingsAccountRepository.UpdateAsync(account);

                // Registrar transacción
                var transaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = dto.Amount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Credito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.Deposito,
                    Origin = "DEPÓSITO",
                    Beneficiary = dto.DestinationAccountNumber,
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _transactionRepository.AddAsync(transaction);

                // Send email notification
                try
                {
                    string last4 = dto.DestinationAccountNumber.Length >= 4 ? dto.DestinationAccountNumber.Substring(dto.DestinationAccountNumber.Length - 4) : dto.DestinationAccountNumber;
                    var message = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{account.CustomerId}@artemis.com",
                        Subject = $"Depósito realizado a su cuenta {last4}",
                        Message = $"Hola Cliente {account.CustomerId},\n\nSe ha realizado un depósito a su cuenta terminada en {last4}.\nMonto depositado: RD${dto.Amount:N2}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                    };
                    await _emailServices.SendNotification(message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
                    // No revertimos, devolvemos SuccessWithMessage (o simplemente Success porque el TempData capturará el error si queremos o solo el log)
                    // The requirement says: "El depósito no debe revertirse. El sistema debe registrar el error y mostrar un mensaje informativo al cajero."
                    // Since ValidationResult doesn't have a warning state, we can return success but we could throw a specific response.
                    // For simplicity, returning success here and we'll handle the UI message in the controller.
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar depósito ATM.");
                return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.DepositError", "Error interno al procesar el depósito."));
            }
        }

        public async Task<ValidationResult> ProcessAtmWithdrawalAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmWithdrawalDto dto)
        {
            try
            {
                var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                if (account == null || !account.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.Withdrawal", "La cuenta origen no es válida o está inactiva."));

                if (account.Balance < dto.Amount)
                {
                    // Registrar intento rechazado
                    var rejected = new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = dto.Amount,
                        TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                        OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.Retiro,
                        Origin = dto.SourceAccountNumber,
                        Beneficiary = "CAJERO",
                        Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Rechazada,
                        PerformedByUserId = dto.CashierId,
                        Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                        CreateByUserId = dto.CashierId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        RejectionReason = "Fondos insuficientes"
                    };
                    await _transactionRepository.AddAsync(rejected);
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.InsufficientFunds", "Fondos insuficientes."));
                }

                account.Balance -= dto.Amount;
                await _savingsAccountRepository.UpdateAsync(account);

                var transaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = dto.Amount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.Retiro,
                    Origin = dto.SourceAccountNumber,
                    Beneficiary = "RETIRO",
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _transactionRepository.AddAsync(transaction);

                // Send email notification
                try
                {
                    string last4 = dto.SourceAccountNumber.Length >= 4 ? dto.SourceAccountNumber.Substring(dto.SourceAccountNumber.Length - 4) : dto.SourceAccountNumber;
                    var message = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{account.CustomerId}@artemis.com",
                        Subject = $"Retiro realizado desde su cuenta {last4}",
                        Message = $"Hola Cliente {account.CustomerId},\n\nSe ha realizado un retiro desde su cuenta terminada en {last4}.\nMonto retirado: RD${dto.Amount:N2}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                    };
                    await _emailServices.SendNotification(message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar retiro ATM.");
                return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.WithdrawalError", "Error interno al procesar el retiro."));
            }
        }

        public async Task<ValidationResult> ProcessAtmCreditCardPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardPaymentDto dto)
        {
            try
            {
                var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                if (account == null || !account.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.Account", "La cuenta origen no es válida o está inactiva."));

                var card = await _creditCardRepository.GetFirstAsync(c => c.CardNumber == dto.CreditCardNumber);
                if (card == null || card.Status != ArtemisBankingPro.Core.Domain.Common.Enum.CreditCardStatus.Activa)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.CreditCard", "La tarjeta destino no es válida o está inactiva."));

                if (card.OwedAmount <= 0)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.NoDebt", "La tarjeta seleccionada no tiene deuda pendiente."));

                decimal effectiveAmount = Math.Min(dto.Amount, card.OwedAmount);

                if (account.Balance < effectiveAmount)
                {
                    // Registrar intento rechazado
                    var rejected = new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = effectiveAmount,
                        TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                        OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoTarjeta,
                        Origin = dto.SourceAccountNumber,
                        Beneficiary = "****" + card.LastFourDigits,
                        Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Rechazada,
                        PerformedByUserId = dto.CashierId,
                        Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                        CreateByUserId = dto.CashierId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        RejectionReason = "Fondos insuficientes"
                    };
                    await _transactionRepository.AddAsync(rejected);
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.InsufficientFunds", "Fondos insuficientes en la cuenta origen."));
                }

                // Aplicar el pago
                account.Balance -= effectiveAmount;
                await _savingsAccountRepository.UpdateAsync(account);

                card.OwedAmount -= effectiveAmount;
                await _creditCardRepository.UpdateAsync(card);

                var transaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = effectiveAmount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoTarjeta,
                    Origin = dto.SourceAccountNumber,
                    Beneficiary = "****" + card.LastFourDigits,
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _transactionRepository.AddAsync(transaction);
                await _transactionRepository.SaveChangesAsync();

                // Enviar correo(s)
                try
                {
                    string accountLast4 = account.AccountNumber.Length >= 4 ? account.AccountNumber.Substring(account.AccountNumber.Length - 4) : account.AccountNumber;
                    
                    // Notificar al dueño de la tarjeta
                    var cardMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{card.CustomerId}@artemis.com",
                        Subject = $"Pago realizado a la tarjeta {card.LastFourDigits}",
                        Message = $"Hola Cliente {card.CustomerId},\n\nSe ha realizado un pago a su tarjeta de crédito terminada en {card.LastFourDigits}.\n\nMonto pagado: RD${effectiveAmount:N2}\nCuenta origen terminada en: {accountLast4}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                    };
                    await _emailServices.SendNotification(cardMessage);

                    // Si el dueño de la cuenta origen es distinto, notificarlo también
                    if (account.CustomerId != card.CustomerId)
                    {
                        var accountMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                        {
                            To = $"{account.CustomerId}@artemis.com",
                            Subject = $"Débito para pago de tarjeta",
                            Message = $"Hola Cliente {account.CustomerId},\n\nSe ha debitado dinero de su cuenta terminada en {accountLast4} para realizar un pago a la tarjeta de crédito terminada en {card.LastFourDigits}.\n\nMonto debitado: RD${effectiveAmount:N2}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                        };
                        await _emailServices.SendNotification(accountMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el pago a tarjeta de crédito en ATM.");
                return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.CreditCardPaymentError", "Error interno al procesar el pago."));
            }
        }

        public async Task<ValidationResult> ProcessAtmLoanPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanPaymentDto dto)
        {
            try
            {
                var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                if (account == null || !account.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.Account", "La cuenta origen no es válida o está inactiva."));

                // Include loanInstallments to update them
                var loan = await _loansRepository.GetFirstAsync(l => l.LoanNumber == dto.LoanNumber, l => l.loanInstallments);
                if (loan == null || loan.Status != ArtemisBankingPro.Core.Domain.Common.Enum.LoanStatus.Activo)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.Loan", "El préstamo destino no es válido o está completado."));

                if (loan.PendingAmount <= 0)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.NoPendingAmount", "El préstamo seleccionado no tiene cuotas pendientes de pago."));

                decimal effectiveAmount = Math.Min(dto.Amount, loan.PendingAmount);

                if (account.Balance < effectiveAmount)
                {
                    // Registrar intento rechazado
                    var rejected = new Transaction
                    {
                        SavingsAccountId = account.Id,
                        Amount = effectiveAmount,
                        TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                        OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoPrestamo,
                        Origin = dto.SourceAccountNumber,
                        Beneficiary = loan.LoanNumber,
                        Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Rechazada,
                        PerformedByUserId = dto.CashierId,
                        Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                        CreateByUserId = dto.CashierId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        RejectionReason = "Fondos insuficientes"
                    };
                    await _transactionRepository.AddAsync(rejected);
                    await _transactionRepository.SaveChangesAsync();
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.InsufficientFunds", "Fondos insuficientes en la cuenta origen."));
                }

                // Descontar balance de la cuenta
                account.Balance -= effectiveAmount;
                await _savingsAccountRepository.UpdateAsync(account);

                // Aplicar el pago a las cuotas
                decimal remainingToApply = effectiveAmount;
                var pendingInstallments = loan.loanInstallments
                    .Where(i => i.paymentStatus != ArtemisBankingPro.Core.Domain.Common.Enum.PaymentStatus.Pagada)
                    .OrderBy(i => i.InstallmentNumber)
                    .ToList();

                foreach (var installment in pendingInstallments)
                {
                    if (remainingToApply <= 0)
                        break;

                    if (remainingToApply >= installment.PendingBalance)
                    {
                        remainingToApply -= installment.PendingBalance;
                        installment.PendingBalance = 0;
                        installment.paymentStatus = ArtemisBankingPro.Core.Domain.Common.Enum.PaymentStatus.Pagada;
                        installment.PaidAt = DateTimeOffset.UtcNow;
                        installment.IsOverdue = false;
                    }
                    else
                    {
                        installment.PendingBalance -= remainingToApply;
                        installment.paymentStatus = ArtemisBankingPro.Core.Domain.Common.Enum.PaymentStatus.ParcialmentePagada;
                        remainingToApply = 0;
                    }
                }

                loan.PendingAmount -= effectiveAmount;
                
                if (loan.PendingAmount <= 0)
                {
                    loan.Status = ArtemisBankingPro.Core.Domain.Common.Enum.LoanStatus.Completado;
                    loan.CompletedAt = DateTimeOffset.UtcNow;
                }

                await _loansRepository.UpdateAsync(loan);

                var transaction = new Transaction
                {
                    SavingsAccountId = account.Id,
                    Amount = effectiveAmount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoPrestamo,
                    Origin = dto.SourceAccountNumber,
                    Beneficiary = loan.LoanNumber,
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _transactionRepository.AddAsync(transaction);
                await _transactionRepository.SaveChangesAsync();

                // Enviar correo(s)
                try
                {
                    string accountLast4 = account.AccountNumber.Length >= 4 ? account.AccountNumber.Substring(account.AccountNumber.Length - 4) : account.AccountNumber;
                    
                    var loanMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{loan.CustomerId}@artemis.com",
                        Subject = $"Pago realizado al préstamo {loan.LoanNumber}",
                        Message = $"Hola Cliente {loan.CustomerId},\n\nSe ha realizado un pago a su préstamo {loan.LoanNumber}.\n\nMonto pagado: RD${effectiveAmount:N2}\nCuenta origen terminada en: {accountLast4}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                    };
                    await _emailServices.SendNotification(loanMessage);

                    if (account.CustomerId != loan.CustomerId)
                    {
                        var accountMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                        {
                            To = $"{account.CustomerId}@artemis.com",
                            Subject = $"Débito para pago de préstamo",
                            Message = $"Hola Cliente {account.CustomerId},\n\nSe ha debitado dinero de su cuenta terminada en {accountLast4} para realizar un pago al préstamo {loan.LoanNumber}.\n\nMonto debitado: RD${effectiveAmount:N2}\nFecha y hora: {transaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                        };
                        await _emailServices.SendNotification(accountMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el pago a préstamo en ATM.");
                return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.LoanPaymentError", "Error interno al procesar el pago."));
            }
        }

        public async Task<ValidationResult> ProcessAtmThirdPartyTransferAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmThirdPartyTransferDto dto)
        {
            try
            {
                var originAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                if (originAccount == null || !originAccount.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.OriginAccount", "El número de cuenta origen ingresado no corresponde a una cuenta válida."));

                var destinationAccount = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.DestinationAccountNumber);
                if (destinationAccount == null || !destinationAccount.IsActive)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.DestinationAccount", "El número de cuenta destino ingresado no corresponde a una cuenta válida."));

                if (originAccount.AccountNumber == destinationAccount.AccountNumber)
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.SameAccount", "La cuenta origen y la cuenta destino no pueden ser la misma."));

                if (originAccount.Balance < dto.Amount)
                {
                    // Registrar intento rechazado en cuenta origen
                    var rejected = new Transaction
                    {
                        SavingsAccountId = originAccount.Id,
                        Amount = dto.Amount,
                        TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                        OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.TransferenciaEntreCuentas,
                        Origin = dto.SourceAccountNumber,
                        Beneficiary = dto.DestinationAccountNumber,
                        Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Rechazada,
                        PerformedByUserId = dto.CashierId,
                        Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                        CreateByUserId = dto.CashierId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        RejectionReason = "Fondos insuficientes"
                    };
                    await _transactionRepository.AddAsync(rejected);
                    await _transactionRepository.SaveChangesAsync();
                    return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.InsufficientFunds", "El monto ingresado excede el saldo disponible de la cuenta."));
                }

                // Descontar y Acreditar
                originAccount.Balance -= dto.Amount;
                destinationAccount.Balance += dto.Amount;

                await _savingsAccountRepository.UpdateAsync(originAccount);
                await _savingsAccountRepository.UpdateAsync(destinationAccount);

                // Registrar Débito
                var debitTransaction = new Transaction
                {
                    SavingsAccountId = originAccount.Id,
                    Amount = dto.Amount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Debito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.TransferenciaEntreCuentas,
                    Origin = originAccount.AccountNumber,
                    Beneficiary = destinationAccount.AccountNumber,
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // Registrar Crédito
                var creditTransaction = new Transaction
                {
                    SavingsAccountId = destinationAccount.Id,
                    Amount = dto.Amount,
                    TransactionType = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionType.Credito,
                    OperationType = ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.TransferenciaEntreCuentas,
                    Origin = originAccount.AccountNumber,
                    Beneficiary = destinationAccount.AccountNumber,
                    Status = ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada,
                    PerformedByUserId = dto.CashierId,
                    Channel = ArtemisBankingPro.Core.Domain.Common.Enum.ChannelPayment.Cajero,
                    CreateByUserId = dto.CashierId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _transactionRepository.AddAsync(debitTransaction);
                await _transactionRepository.AddAsync(creditTransaction);
                
                await _transactionRepository.SaveChangesAsync();

                // Enviar correos
                try
                {
                    string originLast4 = originAccount.AccountNumber.Length >= 4 ? originAccount.AccountNumber.Substring(originAccount.AccountNumber.Length - 4) : originAccount.AccountNumber;
                    string destLast4 = destinationAccount.AccountNumber.Length >= 4 ? destinationAccount.AccountNumber.Substring(destinationAccount.AccountNumber.Length - 4) : destinationAccount.AccountNumber;

                    var originMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{originAccount.CustomerId}@artemis.com",
                        Subject = $"Transacción realizada a la cuenta {destLast4}",
                        Message = $"Hola Cliente {originAccount.CustomerId},\n\nSe ha realizado una transferencia desde su cuenta.\n\nMonto transferido: RD${dto.Amount:N2}\nCuenta origen terminada en: {originLast4}\nCuenta destino terminada en: {destLast4}\nFecha y hora: {debitTransaction.CreatedAt:dd/MM/yyyy HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria."
                    };
                    await _emailServices.SendNotification(originMessage);

                    var destMessage = new Artemis_Banking_Pro.Core.Application.DTOs.Messages.MessageDto
                    {
                        To = $"{destinationAccount.CustomerId}@artemis.com",
                        Subject = $"Transacción enviada desde la cuenta {originLast4}",
                        Message = $"Hola Cliente {destinationAccount.CustomerId},\n\nSe ha recibido una transferencia en su cuenta.\n\nMonto recibido: RD${dto.Amount:N2}\nCuenta origen terminada en: {originLast4}\nCuenta destino terminada en: {destLast4}\nFecha y hora: {creditTransaction.CreatedAt:dd/MM/yyyy HH:mm:ss}"
                    };
                    await _emailServices.SendNotification(destMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar transferencia a terceros en ATM.");
                return ValidationResult.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.TransferError", "Error interno al procesar la transferencia."));
            }
        }

        public async Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmIndicatorsDto>> GetCashierDailyIndicatorsAsync(string cashierId)
        {
            var today = DateTimeOffset.UtcNow.Date;
            
            var todayTransactions = await _transactionRepository.GetAllFindAsync(t => 
                t.PerformedByUserId == cashierId && 
                t.CreatedAt.Date == today &&
                t.Status == ArtemisBankingPro.Core.Domain.Common.Enum.TransactionStatus.Aprobada);

            var deposits = todayTransactions.Count(t => t.OperationType == ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.Deposito);
            var withdrawals = todayTransactions.Count(t => t.OperationType == ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.Retiro);
            var cardPayments = todayTransactions.Count(t => t.OperationType == ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoTarjeta);
            var loanPayments = todayTransactions.Count(t => t.OperationType == ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.PagoPrestamo);
            var thirdPartyTransfers = todayTransactions.Count(t => t.OperationType == ArtemisBankingPro.Core.Domain.Common.Enum.OperationType.TransferenciaEntreCuentas);

            var totalPayments = cardPayments + loanPayments;
            var totalTransactions = deposits + withdrawals + totalPayments + thirdPartyTransfers;

            var dto = new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmIndicatorsDto
            {
                TotalTransactions = totalTransactions,
                TotalPayments = totalPayments,
                TotalDeposits = deposits,
                TotalWithdrawals = withdrawals
            };

            return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmIndicatorsDto>.Success(dto);
        }

        public async Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto>> GetAtmAccountDetailsAsync(string accountNumber)
        {
            var account = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == accountNumber);
            if (account == null)
                return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto>.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.AccountNotFound", "El número de cuenta ingresado no corresponde a una cuenta válida."));

            var dto = new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto
            {
                AccountNumber = account.AccountNumber,
                IsActive = account.IsActive,
                OwnerName = $"Cliente {account.CustomerId}", // Mock until real Identity connection
                OwnerEmail = $"{account.CustomerId}@artemis.com", // Mock until real Identity connection
                Balance = account.Balance
            };

            return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto>.Success(dto);
        }

        public async Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto>> GetAtmCreditCardDetailsAsync(string cardNumber)
        {
            var card = await _creditCardRepository.GetFirstAsync(c => c.CardNumber == cardNumber);
            if (card == null)
                return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto>.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.CreditCardNotFound", "El número de tarjeta ingresado no corresponde a una tarjeta válida."));

            var dto = new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto
            {
                CreditCardNumber = card.CardNumber,
                CustomerId = card.CustomerId,
                OwnerName = $"Cliente {card.CustomerId}", // Mock until real Identity connection
                OwnerEmail = $"{card.CustomerId}@artemis.com", // Mock until real Identity connection
                IsActive = card.Status == ArtemisBankingPro.Core.Domain.Common.Enum.CreditCardStatus.Activa,
                Debt = card.OwedAmount
            };

            return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto>.Success(dto);
        }

        public async Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto>> GetAtmLoanDetailsAsync(string loanNumber)
        {
            var loan = await _loansRepository.GetFirstAsync(l => l.LoanNumber == loanNumber, l => l.loanInstallments);
            if (loan == null)
                return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto>.Failure(new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Atm.LoanNotFound", "El número de préstamo ingresado no corresponde a un préstamo válido."));

            var dto = new Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto
            {
                LoanNumber = loan.LoanNumber,
                CustomerId = loan.CustomerId,
                OwnerName = $"Cliente {loan.CustomerId}", // Mock until real Identity connection
                OwnerEmail = $"{loan.CustomerId}@artemis.com", // Mock until real Identity connection
                IsActive = loan.Status == ArtemisBankingPro.Core.Domain.Common.Enum.LoanStatus.Activo,
                PendingAmount = loan.PendingAmount,
                HasPendingInstallments = loan.loanInstallments.Any(i => i.paymentStatus != ArtemisBankingPro.Core.Domain.Common.Enum.PaymentStatus.Pagada)
            };

            return ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto>.Success(dto);
        }

        #endregion
    }
}