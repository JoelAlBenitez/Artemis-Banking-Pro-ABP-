using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans
{
    public sealed class LoanInstallmentRepository :
        GenericRepository<LoanInstallment, int>,
        ILoanInstallmentRepository
    {
        public LoanInstallmentRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<bool> AddRangeLoansInstallmentAsync(List<LoanInstallment> loans)
        {
            await _context.LoanInstallments.AddRangeAsync(loans);
            return true;
        }

        public Task<bool> UpdateRangeLoansInstallmentAsync(List<LoanInstallment> loans)
        {
            _context.LoanInstallments.UpdateRange(loans);
            return Task.FromResult(true);
        }
    }
}
