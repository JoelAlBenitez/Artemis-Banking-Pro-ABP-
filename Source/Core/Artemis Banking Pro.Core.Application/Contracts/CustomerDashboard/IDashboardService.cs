using Artemis_Banking_Pro.Core.Application.ViewModels.Dashboard;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using Artemis_Banking_Pro.Core.Application.DTOs.CreditCards;
using Artemis_Banking_Pro.Core.Application.DTOs.Loans;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Dashboard
{
    public interface IDashboardService
    {
        Task<ValidationResult<ClientDashboardViewModel>> GetClientDashboardAsync(string clientId);
        Task<ValidationResult<IReadOnlyCollection<TransactionResultDto>>> GetSavingsAccountDetailsAsync(int accountId, string clientId);
        Task<ValidationResult<IReadOnlyCollection<CardConsumptionDto>>> GetCreditCardDetailsAsync(int cardId, string clientId);
        Task<ValidationResult<DetailLoansDto>> GetLoanDetailsAsync(int loanId, string clientId);
    }
}
