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
    public sealed class TransactionService : ITransactionService
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
    }
}