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
            return noTracking ? _entitySet.AsNoTracking() : _entitySet;
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _entitySet.FindAsync(id).ConfigureAwait(false);
        }

        public void Insert(T entity)
        {
            _entitySet.Add(entity);
        }

        public void Insert(IReadOnlyList<T> entities)
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
            // Tracking kept on: callers often mutate the result (OTP mark-used, package status).
            return predicate == null
                ? _entitySet.FirstOrDefaultAsync()
                : _entitySet.FirstOrDefaultAsync(predicate);
        }

        public Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default,
            bool noTracking = false)
        {
            IQueryable<T> query = noTracking ? _entitySet.AsNoTracking() : _entitySet;
            if (predicate != null)
                query = query.Where(predicate);
            return query.ToListAsync(cancellationToken);
        }

        public void Update(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var entry = _context.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                _context.Attach(item);
                entry.State = EntityState.Modified;
                return;
            }

            // Tracked entity: rely on change-detection for property mutations (owned types break if forced Modified).
            if (entry.State == EntityState.Unchanged)
                entry.State = EntityState.Modified;
        }

        public virtual IQueryable<T> FetchMulti(Expression<Func<T, bool>>? predicate = null, bool noTracking = false)
        {
            IQueryable<T> query = noTracking ? _entitySet.AsNoTracking() : _entitySet;
            return predicate == null ? query : query.Where(predicate);
        }

        public virtual async Task<int> BulkUpdateAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, T>> updatePredicate)
        {
            return await _entitySet.Where(predicate).UpdateAsync(updatePredicate).ConfigureAwait(false);
        }

        public async Task<int> ExecuteUpdatePropertyAsync<TProperty>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProperty>> propertyExpression,
            TProperty value,
            CancellationToken cancellationToken = default)
        {
            var entity = await _entitySet.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
            if (entity is null)
                return 0;

            _context.Entry(entity).Property(propertyExpression).CurrentValue = value;
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
