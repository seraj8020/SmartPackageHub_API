namespace SmartPackageHub.Domains.Abstractions.Entities
{
    public class Entity<T>
    {
        public T Id { get; protected set; } = default!;
    }
}
