
namespace SmartPackageHub.Domains.Abstractions.ValueObjects
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();
      
       
    }
}
