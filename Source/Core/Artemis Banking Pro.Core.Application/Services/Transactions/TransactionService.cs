using System.Linq.Expressions;
using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.Contracts.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
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

        public TransactionService(
            ISavingsAccountsRepository savingsAccountRepository,
            ITransactionRepository transactionRepository,
            IBeneficiaryRepository beneficiaryRepository,
            ICreditCardsRepository creditCardRepository,
            ILoansRepository loansRepository,
            ITransactionsValidationServices validationServices,
            IEmailServices emailServices,
            IMapper mapper,
            ILogger<TransactionService> logger)
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
        }

        public async Task<ValidationResult<TransactionResultDto>> ProcessExpressAsync(ExpressTransactionDto dto, string clientId)
        {
            var validation = await _validationServices.ValidateExpressAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    var srcAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);
                    var dstAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.DestinationAccountNumber);

                    if (srcAcc is not null && dstAcc is not null)
                    {
                        var rejectedTx = new Transaction
                        {
                            SavingsAccountId = srcAcc.Id,
                            Amount = dto.Amount,
                            TransactionType = TransactionType.Debito,
                            OperationType = OperationType.TransaccionExpress,
                            Origin = srcAcc.AccountNumber,
                            Beneficiary = dstAcc.AccountNumber,
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

                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            var (originAccount, destAccount) = validation.Value;

            originAccount.Balance -= dto.Amount;
            destAccount.Balance += dto.Amount;

            await _savingsAccountRepository.UpdateAsync(originAccount);
            await _savingsAccountRepository.UpdateAsync(destAccount);

            var debitTx = new Transaction
            {
                SavingsAccountId = originAccount.Id,
                Amount = dto.Amount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.TransaccionExpress,
                Origin = originAccount.AccountNumber,
                Beneficiary = destAccount.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var creditTx = new Transaction
            {
                SavingsAccountId = destAccount.Id,
                Amount = dto.Amount,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.TransaccionExpress,
                Origin = originAccount.AccountNumber,
                Beneficiary = destAccount.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(debitTx);
            await _transactionRepository.AddAsync(creditTx);
            await _transactionRepository.SaveChangesAsync();

            debitTx.RelatedTransactionId = creditTx.Id;
            creditTx.RelatedTransactionId = debitTx.Id;

            await _transactionRepository.UpdateAsync(debitTx);
            await _transactionRepository.UpdateAsync(creditTx);
            await _transactionRepository.SaveChangesAsync();

            // Pendiente: Obtener correos reales desde IIdentityServices cuando Adrian complete el módulo
            var emisorEmail = $"{clientId}@artemis.com";
            var receptorEmail = $"{destAccount.CustomerId}@artemis.com";

            var lastFourDest = destAccount.AccountNumber.Length >= 4 ? destAccount.AccountNumber.Substring(destAccount.AccountNumber.Length - 4) : destAccount.AccountNumber;
            var lastFourOrig = originAccount.AccountNumber.Length >= 4 ? originAccount.AccountNumber.Substring(originAccount.AccountNumber.Length - 4) : originAccount.AccountNumber;

            try
            {
                await _emailServices.SendNotification(new MessageDto
                {
                    To = emisorEmail,
                    Subject = $"Transacción realizada a la cuenta {lastFourDest}",
                    Message = $"Monto: RD${dto.Amount:N2}, Fecha: {debitTx.CreatedAt}, Cuenta Destino: ****{lastFourDest}"
                });

                await _emailServices.SendNotification(new MessageDto
                {
                    To = receptorEmail,
                    Subject = $"Transacción enviada desde la cuenta {lastFourOrig}",
                    Message = $"Monto: RD${dto.Amount:N2}, Fecha: {debitTx.CreatedAt}, Cuenta Origen: ****{lastFourOrig}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.");
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = dto.Amount,
                TransactionType = "DÉBITO",
                Status = "APROBADA",
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        public async Task<ValidationResult<TransactionResultDto>> ProcessBeneficiaryTransactionAsync(BeneficiaryTransactionDto dto, string clientId)
        {
            var validation = await _validationServices.ValidateBeneficiaryAsync(dto, clientId);
            if (!validation.IsValid)
            {
                if (validation.Errors.Contains(TransactionError.InsufficientFunds))
                {
                    var beneficiary = await _beneficiaryRepository.GetFirstAsync(b => b.Id == dto.BeneficiaryId && b.OwnerClientId == clientId);
                    var srcAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == dto.SourceAccountNumber);

                    if (beneficiary is not null && srcAcc is not null)
                    {
                        var dstAcc = await _savingsAccountRepository.GetFirstAsync(a => a.AccountNumber == beneficiary.BeneficiaryAccountNumber);
                        if (dstAcc is not null)
                        {
                            var rejectedTx = new Transaction
                            {
                                SavingsAccountId = srcAcc.Id,
                                Amount = dto.Amount,
                                TransactionType = TransactionType.Debito,
                                OperationType = OperationType.TransaccionBeneficiario,
                                Origin = srcAcc.AccountNumber,
                                Beneficiary = dstAcc.AccountNumber,
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
                }

                return ValidationResult<TransactionResultDto>.Failure(validation.Errors.ToList());
            }

            var (originAccount, destAccount) = validation.Value;

            originAccount.Balance -= dto.Amount;
            destAccount.Balance += dto.Amount;

            await _savingsAccountRepository.UpdateAsync(originAccount);
            await _savingsAccountRepository.UpdateAsync(destAccount);

            var debitTx = new Transaction
            {
                SavingsAccountId = originAccount.Id,
                Amount = dto.Amount,
                TransactionType = TransactionType.Debito,
                OperationType = OperationType.TransaccionBeneficiario,
                Origin = originAccount.AccountNumber,
                Beneficiary = destAccount.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            var creditTx = new Transaction
            {
                SavingsAccountId = destAccount.Id,
                Amount = dto.Amount,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.TransaccionBeneficiario,
                Origin = originAccount.AccountNumber,
                Beneficiary = destAccount.AccountNumber,
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = clientId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = clientId
            };

            await _transactionRepository.AddAsync(debitTx);
            await _transactionRepository.AddAsync(creditTx);
            await _transactionRepository.SaveChangesAsync();

            debitTx.RelatedTransactionId = creditTx.Id;
            creditTx.RelatedTransactionId = debitTx.Id;

            await _transactionRepository.UpdateAsync(debitTx);
            await _transactionRepository.UpdateAsync(creditTx);
            await _transactionRepository.SaveChangesAsync();

            // Pendiente: Obtener correos reales desde IIdentityServices cuando Adrian complete el módulo
            var emisorEmail = $"{clientId}@artemis.com";
            var receptorEmail = $"{destAccount.CustomerId}@artemis.com";

            var lastFourDest = destAccount.AccountNumber.Length >= 4 ? destAccount.AccountNumber.Substring(destAccount.AccountNumber.Length - 4) : destAccount.AccountNumber;
            var lastFourOrig = originAccount.AccountNumber.Length >= 4 ? originAccount.AccountNumber.Substring(originAccount.AccountNumber.Length - 4) : originAccount.AccountNumber;

            try
            {
                await _emailServices.SendNotification(new MessageDto
                {
                    To = emisorEmail,
                    Subject = $"Transacción realizada a la cuenta {lastFourDest}",
                    Message = $"Monto: RD${dto.Amount:N2}, Fecha: {debitTx.CreatedAt}, Cuenta Destino: ****{lastFourDest}"
                });

                await _emailServices.SendNotification(new MessageDto
                {
                    To = receptorEmail,
                    Subject = $"Transacción enviada desde la cuenta {lastFourOrig}",
                    Message = $"Monto: RD${dto.Amount:N2}, Fecha: {debitTx.CreatedAt}, Cuenta Origen: ****{lastFourOrig}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.");
            }

            var resultDto = new TransactionResultDto
            {
                EffectiveAmount = dto.Amount,
                TransactionType = "DÉBITO",
                Status = "APROBADA",
                CreatedAt = debitTx.CreatedAt
            };

            return ValidationResult<TransactionResultDto>.Success(resultDto);
        }

        public async Task<ValidationResult<int>> GetTotalHistoricalAsync()
        {
            var total = await _transactionRepository.CountAsync();
            return ValidationResult<int>.Success(total);
        }

        public async Task<ValidationResult<int>> GetTotalTodayAsync()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var total = await _transactionRepository.CountAsync(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1));
            return ValidationResult<int>.Success(total);
        }

        public async Task<ValidationResult> RegisterInitialTransactionAsync(int savingsAccountId, decimal amount, string performedByUserId)
        {
            if (amount <= 0)
            {
                return ValidationResult.Failure(TransactionError.InvalidAmount);
            }

            var transaction = new Transaction
            {
                SavingsAccountId = savingsAccountId,
                Amount = amount,
                TransactionType = TransactionType.Credito,
                OperationType = OperationType.AperturaCuenta,
                Origin = "DEPÓSITO APERTURA",
                Status = TransactionStatus.Aprobada,
                PerformedByUserId = performedByUserId,
                Channel = ChannelPayment.Cliente,
                CreatedAt = DateTimeOffset.UtcNow,
                CreateByUserId = performedByUserId
            };

            await _transactionRepository.AddAsync(transaction);
            return ValidationResult.Success();
        }

        public async Task<ValidationResult<IReadOnlyCollection<ClientDto>>> GetClientsAsync()
        {
            // Pendiente: Integrar con IIdentityServices cuando Adrian complete el módulo para obtener clientes reales activos e inactivos
            var accounts = await _savingsAccountRepository.GetAllFindAsync(a => true);
            var cards = await _creditCardRepository.GetAllFindAsync(c => true);
            var loans = await _loansRepository.GetAllFindAsync(l => true);

            var customerIds = accounts.Select(a => a.CustomerId)
                .Concat(cards.Select(c => c.CustomerId))
                .Concat(loans.Select(l => l.CustomerId))
                .Distinct()
                .ToList();

            var clients = customerIds.Select(id => new ClientDto
            {
                Id = id,
                IdCard = "001-0000000-1",
                FullName = $"Cliente {id}",
                Email = $"{id}@artemis.com",
                IsActive = true
            }).ToList();

            return ValidationResult<IReadOnlyCollection<ClientDto>>.Success(clients);
        }

        public async Task<ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>> GetBeneficiariesAsync(string clientId)
        {
            var beneficiaries = await _beneficiaryRepository.GetAllFindAsync(b => b.OwnerClientId == clientId && b.IsActive);
            var dtos = _mapper.Map<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>(beneficiaries);
            return ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>.Success(dtos);
        }
    }
}
