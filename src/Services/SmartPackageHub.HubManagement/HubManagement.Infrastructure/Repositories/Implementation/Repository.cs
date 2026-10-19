using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.Linq.Expressions;
using Z.EntityFramework.Plus;

namespace SmartPackageHub.HubManagement.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : EntityBase
    {
        private readonly HubDbContext _context;
        private readonly DbSet<T> _entitySet;

        public Repository(HubDbContext context)
        {
            _context = context;
            _entitySet = _context.Set<T>();
        }

        public IQueryable<T> GetAll(bool noTracking = true)
        {
            var set = _entitySet;
            if (noTracking)
            {
                return set.AsNoTracking();
            }
            return set;
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _entitySet.FindAsync(id);
        }

        public void Insert(T entity)
        {
            _entitySet.Add(entity);
        }

        public void Insert(List<T> entities)
        {
            _entitySet.AddRange(entities);
        }

        public void Delete(T entity)
        {
            _entitySet.Remove(entity);
        }

        public void Remove(IEnumerable<T> entitiesToRemove)
        {
            _entitySet.RemoveRange(entitiesToRemove);
        }

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? _context.Set<T>().FirstOrDefaultAsync() : _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public void Update(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var entry = _context.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                _context.Attach(item);
                entry.State = EntityState.Modified;
            }
        }

        public virtual IQueryable<T> FetchMulti(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? _context.Set<T>() : _context.Set<T>().Where(predicate);
        }

        public virtual async Task<int> BulkUpdateAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, T>> updatePredicate)
        {
            return await _context.Set<T>().Where(predicate).UpdateAsync(updatePredicate);
        }
    }
}
