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

    }
}
