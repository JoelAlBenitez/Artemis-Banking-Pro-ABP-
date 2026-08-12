using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Commerces;
using ArtemisBankingPro.Core.Domain.Interfaces.Commerces;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Commerces
{
    public sealed class CommerceRepository :
        GenericRepository<Commerce, int>,
        ICommerceRepository
    {
        public CommerceRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<PagedResult<Commerce>> GetPagedCommercesAsync(
            int page,
            int pageSize,
            CommerceStatus? status)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<Commerce> query = _context.Commerces.AsNoTracking();

            if (status is not null)
                query = query.Where(commerce => commerce.Status == status);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(commerce => commerce.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Commerce>(items, page, pageSize, totalRecords);
        }
    }
}
