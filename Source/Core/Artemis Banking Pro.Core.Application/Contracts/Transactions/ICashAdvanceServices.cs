using Artemis_Banking_Pro.Core.Application.DTOs.Transactions;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Transactions
{
    public interface ICashAdvanceServices
    {
        Task<ValidationResult<CashAdvanceDto>> ProcessCashAdvanceAsync(
            CashAdvanceRequestDto dto, 
            string clientId);
    }
}
