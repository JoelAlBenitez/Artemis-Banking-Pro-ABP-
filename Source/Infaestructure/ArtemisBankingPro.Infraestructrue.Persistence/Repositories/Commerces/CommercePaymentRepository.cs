using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Commerces
{
    public sealed class CommercePaymentRepository :
        GenericRepository<CommercePayment, int>,
        ICommercePaymentRepository
    {
        public CommercePaymentRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<PagedResult<CommercePayment>> GetPagedPaymentsByCommerceAsync(
            int commerceId,
            int page,
            int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<CommercePayment> query = _context.CommercePayments
                .AsNoTracking()
                .Where(payment => payment.CommerceId == commerceId);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(payment => payment.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CommercePayment>(items, page, pageSize, totalRecords);
        }
    }
}
