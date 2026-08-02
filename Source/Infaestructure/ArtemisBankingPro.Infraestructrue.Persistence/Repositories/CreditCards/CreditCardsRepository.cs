using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Enum;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.CreditCards;
using ArtemisBankingPro.Core.Domain.Interfaces.CreditCards;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.CreditCards
{
    public sealed class CreditCardsRepository :
        GenericRepository<CreditCard, int>,
        ICreditCardsRepository
    {
        public CreditCardsRepository(DbContextArtemisBanking context) : base(context) { }

        public async Task<PagedResult<CreditCard>> GetPagedCreditCardsAsync(
            int page,
            int pageSize,
            CreditCardStatus? status,
            string? customerId)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<CreditCard> query = _context.CreditCards.AsNoTracking();

            if (status is not null) query = query.Where(card => card.Status == status);
            if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(card => card.CustomerId == customerId);

            var totalRecords = await query.CountAsync();

            //Sin filtro de estado las activas se muestran primero; dentro de cada grupo,
            //de la más reciente a la más antigua.
            query = status is null
                ? query.OrderBy(card => card.Status).ThenByDescending(card => card.CreatedAt)
                : query.OrderByDescending(card => card.CreatedAt);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CreditCard>(items, page, pageSize, totalRecords);
        }
    }
}
