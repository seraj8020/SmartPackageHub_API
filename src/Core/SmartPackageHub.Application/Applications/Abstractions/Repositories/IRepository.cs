using SmartPackageHub.Domains.Abstractions.Entities;
using System.Linq.Expressions;

namespace SmartPackageHub.Applications.Abstractions.Repositories
{
    public interface IRepository<T> where T : EntityBase
    {
        IQueryable<T> GetAll(bool noTracking = true);
        Task<T?> GetByIdAsync(Guid id);
        void Insert(T entity);
        void Insert(IReadOnlyList<T> entities);
        void Delete(T entity);
        void Remove(IEnumerable<T> entitiesToRemove);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>>? predicate = null);
        /// <param name="noTracking">
        /// Use true for read-only lists (faster, no change-tracker). Keep false when entities will be updated.
        /// </param>
        Task<List<T>> ListAsync(
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default,
            bool noTracking = false);
        void Update(T item);
        IQueryable<T> FetchMulti(Expression<Func<T, bool>>? predicate = null, bool noTracking = false);

        /// <summary>EF Core ExecuteUpdate for a single column (avoids owned-type tracking issues).</summary>
        Task<int> ExecuteUpdatePropertyAsync<TProperty>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProperty>> propertyExpression,
            TProperty value,
            CancellationToken cancellationToken = default);
    }
}
