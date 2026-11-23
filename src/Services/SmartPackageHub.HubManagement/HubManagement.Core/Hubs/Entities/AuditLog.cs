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

        /// <summary>Optional JSON/text payload (e.g. original shelf, return reason, staff extras).</summary>
        public string? Details { get; private set; }

        private AuditLog(
            Guid societyId,
            Guid userId,
            string entityName,
            Guid entityId,
            string action,
            DateTime executedAt,
            string? details)
        {
            SocietyId = societyId;
            UserId = userId;
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            ExecutedAt = executedAt;
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        }

        public static AuditLog Create(
            Guid societyId,
            Guid userId,
            string entityName,
            Guid entityId,
            string action,
            DateTime executedAt,
            string? details = null)
        {
            return new AuditLog(societyId, userId, entityName, entityId, action, executedAt, details);
        }
    }
}
