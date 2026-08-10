using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Loans
{
    public interface ILoanInstallmentRepository :
        IGenericRepository<LoanInstallment, int>
    {
        Task<bool> AddRangeLoansInstallmentAsync(List<LoanInstallment> loans);
        Task<bool> UpdateRangeLoansInstallmentAsync(List<LoanInstallment> loans);
    }
}
