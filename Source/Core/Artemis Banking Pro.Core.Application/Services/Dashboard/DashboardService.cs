using Artemis_Banking_Pro.Core.Application.Contracts.Dashboard;
using Artemis_Banking_Pro.Core.Application.ViewModels.Dashboard;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using Artemis_Banking_Pro.Core.Application.DTOs.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.CodeErrors.CustomerErros;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using AutoMapper;

namespace Artemis_Banking_Pro.Core.Application.Services.Dashboard
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly ISavingsAccountRepository _savingsAccountRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly ILoansRepository _loansRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardConsumptionRepository _cardConsumptionRepository;
        private readonly IMapper _mapper;

        public DashboardService(
            ISavingsAccountRepository savingsAccountRepository,
            ICreditCardRepository creditCardRepository,
            ILoansRepository loansRepository,
            ITransactionRepository transactionRepository,
            ICardConsumptionRepository cardConsumptionRepository,
            IMapper mapper)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _creditCardRepository = creditCardRepository;
            _loansRepository = loansRepository;
            _transactionRepository = transactionRepository;
            _cardConsumptionRepository = cardConsumptionRepository;
            _mapper = mapper;
        }

        public async Task<ValidationResult<ClientDashboardViewModel>> GetClientDashboardAsync(string clientId)
        {
            try
            {
                // 1. Obtener cuentas de ahorro activas del cliente
                var accounts = await _savingsAccountRepository.GetAllFindAsync(
                    a => a.ClientId == clientId && a.Status == SavingsAccountStatus.Activa
                );

                // Regla: La cuenta principal siempre primero; secundarias ordenadas de mayor a menor balance
                var sortedAccounts = accounts
                    .OrderByDescending(a => a.Type == SavingsAccountType.Principal)
                    .ThenByDescending(a => a.Balance)
                    .ToList();

                // 2. Obtener préstamos activos del cliente
                var loans = await _loansRepository.GetAllFindAsync(
                    l => l.CustomerId == clientId && l.Status == LoanStatus.Activo,
                    l => l.loanInstallments
                );

                // 3. Obtener tarjetas de crédito activas del cliente
                var cards = await _creditCardRepository.GetAllFindAsync(
                    c => c.ClientId == clientId && c.Status == CreditCardStatus.Activa
                );

                // Mapear a DTOs correspondientes
                var accountsMapped = _mapper.Map<IReadOnlyCollection<SavingsAccountDto>>(sortedAccounts);
                var loansMapped = _mapper.Map<IReadOnlyCollection<LoansDto>>(loans);
                var cardsMapped = _mapper.Map<IReadOnlyCollection<CreditCardDto>>(cards);

                // Construir el ViewModel para el dashboard
                var dashboardViewModel = new ClientDashboardViewModel
                {
                    SavingsAccounts = accountsMapped,
                    Loans = loansMapped,
                    CreditCards = cardsMapped
                };

                return ValidationResult<ClientDashboardViewModel>.Success(dashboardViewModel);
            }
            catch (Exception)
            {
                return ValidationResult<ClientDashboardViewModel>.Failure(
                    new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Error", "Ha ocurrido un error al cargar el dashboard del cliente.")
                );
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<TransactionResultDto>>> GetSavingsAccountDetailsAsync(int accountId, string clientId)
        {
            try
            {
                // Validar la cuenta y propiedad del cliente
                var account = await _savingsAccountRepository.GetFirstAsync(a => a.Id == accountId);
                if (account == null || account.ClientId != clientId)
                {
                    return ValidationResult<IReadOnlyCollection<TransactionResultDto>>.Failure(DashboardError.AccountNotFound);
                }

                // Obtener el historial de transacciones (ordenadas por fecha de creación desc)
                var transactions = await _transactionRepository.GetAllFindAsync(t => t.SavingsAccountId == accountId);
                var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt).ToList();

                var mapped = _mapper.Map<IReadOnlyCollection<TransactionResultDto>>(sortedTransactions);
                return ValidationResult<IReadOnlyCollection<TransactionResultDto>>.Success(mapped);
            }
            catch (Exception)
            {
                return ValidationResult<IReadOnlyCollection<TransactionResultDto>>.Failure(
                    new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Error", "Ha ocurrido un error al obtener los detalles de la cuenta de ahorros.")
                );
            }
        }

        public async Task<ValidationResult<IReadOnlyCollection<CardConsumptionDto>>> GetCreditCardDetailsAsync(int cardId, string clientId)
        {
            try
            {
                // Validar la tarjeta y propiedad del cliente
                var card = await _creditCardRepository.GetFirstAsync(c => c.Id == cardId);
                if (card == null || card.ClientId != clientId)
                {
                    return ValidationResult<IReadOnlyCollection<CardConsumptionDto>>.Failure(DashboardError.CardNotFound);
                }

                // Obtener historial de consumos de la tarjeta (ordenadas por fecha desc)
                var consumptions = await _cardConsumptionRepository.GetAllFindAsync(c => c.CreditCardId == cardId);
                var sortedConsumptions = consumptions.OrderByDescending(c => c.Date).ToList();

                var mapped = _mapper.Map<IReadOnlyCollection<CardConsumptionDto>>(sortedConsumptions);
                return ValidationResult<IReadOnlyCollection<CardConsumptionDto>>.Success(mapped);
            }
            catch (Exception)
            {
                return ValidationResult<IReadOnlyCollection<CardConsumptionDto>>.Failure(
                    new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Error", "Ha ocurrido un error al obtener los detalles de la tarjeta de crédito.")
                );
            }
        }

        public async Task<ValidationResult<DetailLoansDto>> GetLoanDetailsAsync(int loanId, string clientId)
        {
            try
            {
                // Validar el préstamo y propiedad del cliente cargando sus cuotas de amortización
                var loan = await _loansRepository.GetFirstAsync(
                    l => l.Id == loanId && l.CustomerId == clientId,
                    l => l.loanInstallments
                );

                if (loan == null)
                {
                    return ValidationResult<DetailLoansDto>.Failure(DashboardError.LoanNotFound);
                }

                var mapped = _mapper.Map<DetailLoansDto>(loan);
                return ValidationResult<DetailLoansDto>.Success(mapped);
            }
            catch (Exception)
            {
                return ValidationResult<DetailLoansDto>.Failure(
                    new ArtemisBankingPro.Core.Domain.Common.Errors.Error("Error", "Ha ocurrido un error al obtener los detalles del préstamo.")
                );
            }
        }
    }
}
