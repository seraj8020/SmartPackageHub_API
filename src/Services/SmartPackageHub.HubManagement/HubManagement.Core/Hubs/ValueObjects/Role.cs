using HubManagement.Core.Hubs.Models;
using SmartPackageHub.Domains.Abstractions.ValueObjects;


namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class Role : ValueObject
    {
        public RoleType Type { get; private set; }

        public static Role Create(RoleType type)
        {
            return new Role(type);
        }
        private Role(RoleType type) => Type = type;
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Type;
        }
    }
}
