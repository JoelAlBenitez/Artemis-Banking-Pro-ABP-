using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface ITransactionService
    {
        Task<ValidationResult<TransactionResultDto>> ProcessExpressAsync(ExpressTransactionDto dto, string clientId);
        Task<ValidationResult<TransactionResultDto>> ProcessBeneficiaryTransactionAsync(BeneficiaryTransactionDto dto, string clientId);
        Task<ValidationResult<int>> GetTotalHistoricalAsync();
        Task<ValidationResult<int>> GetTotalTodayAsync();
        Task<ValidationResult> RegisterInitialTransactionAsync(InitialTransactionDto dto);
        Task<ValidationResult<IReadOnlyCollection<ClientDto>>> GetClientsAsync();
        Task<ValidationResult<IReadOnlyCollection<Artemis_Banking_Pro.Core.Application.DTOs.Beneficiaries.BeneficiaryDto>>> GetBeneficiariesAsync(string clientId);

        // ATM Methods
        Task<ValidationResult> ProcessAtmDepositAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmDepositDto dto);
        Task<ValidationResult> ProcessAtmWithdrawalAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmWithdrawalDto dto);
        Task<ValidationResult> ProcessAtmCreditCardPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardPaymentDto dto);
        Task<ValidationResult> ProcessAtmLoanPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanPaymentDto dto);
        Task<ValidationResult> ProcessAtmThirdPartyTransferAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmThirdPartyTransferDto dto);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmIndicatorsDto>> GetCashierDailyIndicatorsAsync(string cashierId);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto>> GetAtmAccountDetailsAsync(string accountNumber);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto>> GetAtmCreditCardDetailsAsync(string cardNumber);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto>> GetAtmLoanDetailsAsync(string loanNumber);
    }
}
