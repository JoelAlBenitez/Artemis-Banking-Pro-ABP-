using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans
{
    public sealed class LoansPaymentRepository : ILoansPaymentRepository
    {
        private readonly DbContextArtemisBanking _context;

        public LoansPaymentRepository(DbContextArtemisBanking context)
        {
            _context = context;
        }

        public async Task<bool> AddAsync(LoanPayment loanPayment)
        {
            await _context.LoanPayments.AddAsync(loanPayment);
            return true;
        }
    }
}
