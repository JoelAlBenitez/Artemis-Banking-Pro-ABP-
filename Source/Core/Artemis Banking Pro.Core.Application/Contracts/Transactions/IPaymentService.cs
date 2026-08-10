using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface IPaymentService
    {
        Task<ValidationResult<TransactionResultDto>> PayCreditCardAsync(PayCreditCardDto dto, string clientId);
        Task<ValidationResult<TransactionResultDto>> PayLoanAsync(PayLoanDto dto, string clientId);
    }
}
