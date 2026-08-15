using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface IAtmTransactionService
    {
        Task<ValidationResult> ProcessAtmDepositAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmDepositDto dto);
        Task<ValidationResult> ProcessAtmWithdrawalAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmWithdrawalDto dto);
        Task<ValidationResult> ProcessAtmCreditCardPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardPaymentDto dto);
        Task<ValidationResult> ProcessAtmLoanPaymentAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanPaymentDto dto);
        Task<ValidationResult> ProcessAtmThirdPartyTransferAsync(Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmThirdPartyTransferDto dto);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmIndicatorsDto>> GetAtmCashierDailyIndicatorsAsync(string cashierId);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmAccountDetailsDto>> GetAtmAccountDetailsAsync(string accountNumber);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmCreditCardDetailsDto>> GetAtmCreditCardDetailsAsync(string cardNumber);
        Task<ValidationResult<Artemis_Banking_Pro.Core.Application.DTOs.Transactions.Atm.AtmLoanDetailsDto>> GetAtmLoanDetailsAsync(string loanNumber);
    }
}
