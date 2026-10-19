using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class AuditLog : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid UserId { get; private set; }
        public string EntityName { get; private set; }
        public Guid EntityId { get; private set; }
        public string Action { get; private set; }
        public DateTime ExecutedAt { get; private set; }

        private AuditLog(Guid societyId, Guid userId, string entityName, Guid entityId, string action, DateTime executedAt)
        {
            SocietyId = societyId;
            UserId = userId;
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            ExecutedAt = executedAt;
        }
        public static AuditLog Create(Guid societyId, Guid userId, string entityName, Guid entityId, string action,DateTime executedAt)
        {
            return new AuditLog(societyId, userId, entityName, entityId, action, executedAt);
        }

    }
}
