using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repository
{
    public class GenericCrudService<TEntity, TKeyType> : IGenericCrudService<TEntity, TKeyType> where TEntity : class
    {
        protected readonly MyFeaturesDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericCrudService(MyFeaturesDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        //proć ovo sve detaljno vidit što šta znači

        public async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? skip = null,
            int? take = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(TKeyType id)
        {
            //first or default ovdje ili ovako?
            return await _dbSet.FindAsync(id);
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(TKeyType id)
        {
            var entity = _dbSet.Find(id); // Or use 'FindAsync' in an async method context
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
