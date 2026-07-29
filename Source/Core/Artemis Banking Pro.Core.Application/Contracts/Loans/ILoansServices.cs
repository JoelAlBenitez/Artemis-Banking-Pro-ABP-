using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface ILoansServices
    {
        Task<ValidationResult<PagedResult<LoansDto>>> GetPagedLoansAsync(
            LoansFilterDto filter, string? customerId);

        Task<ValidationResult<DetailLoansDto>> GetDetailLoanAsync(int loanId);

        Task<ValidationResult<EditAnnualInterestRateDto>> GetLoanForEditRateAsync(int loanId);

        Task<ValidationResult<LoanRateUpdatedDto>> EditAnnualInterestRateAsync(
            EditAnnualInterestRateDto dto, string adminUserId);

        Task<ValidationResult> SendRateUpdateNotificationAsync(
            LoanRateUpdatedDto rateUpdated, string customerEmail, string customerFullName);
    }
}
