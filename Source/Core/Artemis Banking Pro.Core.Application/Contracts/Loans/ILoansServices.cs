using Artemis_Banking_Pro.Core.Application.DTOs.Loans;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Common.ValidationResult;
using Artemis_Banking_Pro.Core.Application.Contracts.Generic;

namespace Artemis_Banking_Pro.Core.Application.Contracts.Loans
{
    public interface ILoansServices : IGenericServices<LoansAssignmentDto, LoansDto, int>
    {
        Task<ValidationResult<PagedResult<LoansDto>>> GetPagedLoansAsync(LoansFilterDto filter);
        Task<ValidationResult<DetailLoansDto>> GetDetailLoanAsync(int loanId);
        Task<ValidationResult<EditAnnualInterestRateDto>> GetLoanForEditRateAsync(int loanId);
        Task<ValidationResult> EditAnnualInterestRateAsync(EditAnnualInterestRateDto dto);
        Task<ValidationResult<ClientsForLoanAssignmentDto>> GetCustomersForAssignmentAsync(string? idCard);
        Task<ValidationResult<LoanRiskEvaluationDto>> EvaluateRiskAsync(LoansAssignmentDto dto);
        Task<ValidationResult<IReadOnlyCollection<LoansDto>>> GetActiveLoansByCustomerAsync(string customerId);
    }
}
