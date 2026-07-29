using ArtemisBankingPro.Core.Domain.Common.Constants;
using ArtemisBankingPro.Core.Domain.Common.Pagination;
using ArtemisBankingPro.Core.Domain.Entities.Base;
using ArtemisBankingPro.Core.Domain.Interfaces.Generic;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace ArtemisBankingPro.Infraestructrue.Persistence.Repositories.Generic
{
    public abstract class GenericRepository<TEntity, TKey> :
        IGenericRepository<TEntity, TKey>
        where TEntity : BaseEntitie<TKey>
    {

        protected readonly DbContextArtemisBanking _context;

        public GenericRepository(DbContextArtemisBanking context)
        {
            _context = context;
        }

        //No persiste: la confirmación la ejecuta el servicio con un único SaveChangesAsync
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            return entity;
        }

        public virtual async Task<int> CountAsync(Expression<
            Func<TEntity, bool>>? predicate = null)
        {
            return predicate is null
                ? await _context.Set<TEntity>().CountAsync()
                : await _context.Set<TEntity>().CountAsync(predicate);
        }

        public virtual async Task<bool> ExistElementByConsult(Expression<Func<TEntity, bool>> predicate)
        {
              return await _context.Set<TEntity>().AnyAsync(predicate);
        }

        public virtual async Task<PagedResult<TEntity>> GetAllAsync(int page, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>,
            IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 1, DomainConstants.MaxPageSize);

            IQueryable<TEntity> query =
                  _context.Set<TEntity>().AsNoTracking();

            foreach (var include in includes) query = query.Include(include);
            if (filter is not null) query = query.Where(filter);

            var totalRecords = await query.CountAsync();

            //Orden por defecto: del más reciente al más antiguo
            query = orderBy is not null
                ? orderBy(query)
                : query.OrderByDescending(x => x.CreatedAt);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>(items, page, pageSize, totalRecords);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllFindAsync(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>()
                 .AsNoTracking()
                 .Where(predicate);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return  await query.ToListAsync();
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey key)
        {
            var result =  await _context.Set<TEntity>().FindAsync(key);
            if (result is null) return null!;
            return result!;
        }

        public virtual async Task<TEntity?> GetFirstAsync(
                Expression<Func<TEntity, bool>> predicate,
                params Expression<Func<TEntity, object>>[] includes)
            {

            IQueryable<TEntity> query = _context.Set<TEntity>()
                                        .AsNoTracking();

            foreach(var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate);
            }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<decimal> SumAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, decimal>> selector)
        {
           return await _context.Set<TEntity>().AsNoTracking()
                .Where(predicate)
                .SumAsync(selector);


        }

        //No persiste: la confirmación la ejecuta el servicio con un único SaveChangesAsync
        public virtual Task<bool> UpdateAsync(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
            return Task.FromResult(true);
        }
    }
}
