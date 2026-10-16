using SmartPackageHub.Domains.Abstractions.Entities;
using System.Linq.Expressions;

namespace SmartPackageHub.Applications.Abstractions.Repositories
{
    public interface IRepository<T> where T : EntityBase
    {
        IQueryable<T> GetAll(bool noTracking = true);
        Task<T?> GetByIdAsync(Guid id);
        void Insert(T entity);
        void Insert(List<T> entities);
        void Delete(T entity);
        void Remove(IEnumerable<T> entitiesToRemove);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate = null);
        void Update(T item);
        IQueryable<T> FetchMulti(Expression<Func<T, bool>> predicate = null);
    }
}
