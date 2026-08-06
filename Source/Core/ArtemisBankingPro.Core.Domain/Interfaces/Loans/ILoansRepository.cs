using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Loans
{
    public interface ILoansRepository :
        IGenericRepository<Loan, int>
    {
        Task<PagedResult<Loan>> GetPagedLoansAsync(
            int page,
            int pageSize,
            LoanStatus? status,
            string? customerId);

       
    }
}
