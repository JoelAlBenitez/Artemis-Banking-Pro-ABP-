namespace ArtemisBankingPro.Core.Domain.Interfaces.Generic
{
    public interface IGenericRepository<TEntity, TKey>
    {
        Task AddAsync(TEntity entity);
        Task<int> SaveChangesAsync();
        Task<TEntity> GetByIdAsync(TKey key);
        Task<IReadOnlyCollection<TEntity>> GetAllEntitys();
        Task<bool> UpdateAsync(TEntity entity);
    }
}
