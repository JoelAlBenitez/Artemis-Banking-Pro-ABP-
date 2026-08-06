using ArtemisBankingPro.Core.Domain.Entities.Loans;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Loans
{
    public interface ILoansPaymentRepository
    {
        Task<bool> AddAsync(LoanPayment loanPayment);
    }
}
