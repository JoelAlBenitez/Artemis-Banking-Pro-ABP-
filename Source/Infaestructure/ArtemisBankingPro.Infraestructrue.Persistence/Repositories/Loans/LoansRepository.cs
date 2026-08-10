using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Loans;
using ArtemisBankingPro.Core.Domain.Interfaces.Loans;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Loans
{
    public sealed class LoansRepository :
        GenericRepository<Loan, int>,
        ILoansRepository
    {
        public LoansRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<PagedResult<Loan>> GetPagedLoansAsync(
            int page,
            int pageSize,
            LoanStatus? status,
            string? customerId)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<Loan> query = _context.Loans
                .AsNoTracking()
                .Include(l => l.loanInstallments);

            if (status is not null) query = query.Where(l => l.Status == status);
            if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(l => l.CustomerId == customerId);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(l => l.Status)
                .ThenByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Loan>(items, page, pageSize, totalRecords);
        }

       
    }
}
