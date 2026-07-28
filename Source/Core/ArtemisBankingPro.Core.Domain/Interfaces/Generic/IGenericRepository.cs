using ArtemisBankingPro.Core.Domain.Entities.Base;
using System.Linq.Expressions;

namespace ArtemisBankingPro.Core.Domain.Interfaces.Generic
{
    public interface IGenericRepository<TEntity, TKey>
        where TEntity 
        : BaseEntitie<TKey>
    {

        //Write consults
        Task<TEntity> AddAsync(TEntity entity);
        Task<bool> UpdateAsync(TEntity entity);

        //Reads Consults
        Task<TEntity> GetByIdAsync(TKey key);

        Task<IReadOnlyCollection<TEntity>> GetAllAsync(
           int page, int pageSize,
           Expression<Func<TEntity, bool>>? filter = null,
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
           params Expression<Func<TEntity, object>>[] includes);

        Task<IReadOnlyList<TEntity>> GetAllFindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity?> GetFirstAsync(
            Expression<Func<TEntity, bool>> predicate, 
            params Expression<Func<TEntity, object>>[] includes );

        //Exists Consults
        Task<bool> ExistElementByConsult(Expression<Func<TEntity, bool>> predicate);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null!);

        //Sum Data 
        Task<decimal> SumAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, decimal>> selector);

        //Execute
        Task<int> SaveChangesAsync();

    }
}
