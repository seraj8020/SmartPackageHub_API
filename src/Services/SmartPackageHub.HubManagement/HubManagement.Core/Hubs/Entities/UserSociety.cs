using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class UserSociety : EntityBase
    {
        public Guid UserId { get; private set; }
        public Guid SocietyId { get; private set; }
        private UserSociety(Guid userId, Guid societyId)
        {
            UserId = userId;
            SocietyId = societyId;
        }
        public static UserSociety Create(Guid userId, Guid societyId)
        {
            return new UserSociety(userId, societyId);
        }
    }
}
