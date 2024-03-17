using System.Linq.Expressions;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IGenericCrudService<TEntity, TKeyType> where TEntity : class
    {
        // Asynchronous read operations
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? skip = null,
            int? take = null);
        Task<TEntity> GetByIdAsync(TKeyType id);

        // Synchronous write operations
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Delete(TKeyType id);

        // Asynchronous operation to commit changes to the database
        Task SaveAsync();
    }
}
