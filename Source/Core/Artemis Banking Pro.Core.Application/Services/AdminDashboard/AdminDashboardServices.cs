using Artemis_Banking_Pro.Core.Application.Contracts.AdminDashboard;
using Artemis_Banking_Pro.Core.Application.DTOs.AdminDashboard;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Interfaces.Transactions;
using Microsoft.Extensions.Logging;

namespace Artemis_Banking_Pro.Core.Application.Services.AdminDashboard
{
    public sealed class AdminDashboardServices : IAdminDashboardServices
    {

        private readonly ILogger<AdminDashboardServices> _logger;
        private readonly ILoansRepository _loansRepository;
        private readonly ISavingsAccountsRepository _savingsAccountsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardsRepository _creditCardsRepository;
        //agregar interface que maneja usuarios y Icurrent user services

        public AdminDashboardServices(ILogger<AdminDashboardServices> logger,
            ILoansRepository loansRepository,
            ISavingsAccountsRepository savings,
            ICreditCardsRepository creditCardsRepository,
            ITransactionRepository transactionRepository
            )
        {
            _creditCardsRepository = creditCardsRepository;
            _loansRepository = loansRepository;
            _savingsAccountsRepository = savings;
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task<ValidationResult<AdminDashboardDto>> GetDataAdminDashboard()
        {
            try
            {
                //en espera de ser completado cuando se terminen de hacer las integraciones de lugar

                return null!;
            }catch(Exception ex)
            {
                return null!;
            }
        }
    }
}
