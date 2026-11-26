using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>Resident in-app support ticket (Traizr-style ask-with-context).</summary>
    public sealed class SupportRequest : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }
        public string Topic { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public string? ResidentName { get; private set; }
        public string? ResidentPhone { get; private set; }
        public string? TowerWing { get; private set; }
        public string? FlatNumber { get; private set; }
        public string Status { get; private set; } = "Open";
        public string SupportEmail { get; private set; } = string.Empty;
        public DateTime RequestedAtUtc { get; private set; }

        private SupportRequest()
        {
        }

        private SupportRequest(
            Guid societyId,
            Guid residentId,
            string topic,
            string message,
            string? residentName,
            string? residentPhone,
            string? towerWing,
            string? flatNumber,
            string supportEmail,
            DateTime requestedAtUtc)
        {
            SocietyId = societyId;
            ResidentId = residentId;
            Topic = topic;
            Message = message;
            ResidentName = residentName;
            ResidentPhone = residentPhone;
            TowerWing = towerWing;
            FlatNumber = flatNumber;
            SupportEmail = supportEmail;
            RequestedAtUtc = requestedAtUtc;
            Status = "Open";
        }

        public static SupportRequest Create(
            Guid societyId,
            Guid residentId,
            string topic,
            string message,
            string supportEmail,
            string? residentName = null,
            string? residentPhone = null,
            string? towerWing = null,
            string? flatNumber = null)
        {
            if (societyId == Guid.Empty)
                throw new ArgumentException("Society is required.", nameof(societyId));
            if (residentId == Guid.Empty)
                throw new ArgumentException("Resident is required.", nameof(residentId));

            var cleanTopic = (topic ?? string.Empty).Trim();
            if (cleanTopic.Length is < 2 or > 80)
                throw new ArgumentException("Topic must be between 2 and 80 characters.", nameof(topic));

            var cleanMessage = (message ?? string.Empty).Trim();
            if (cleanMessage.Length is < 8 or > 4000)
                throw new ArgumentException("Message must be between 8 and 4000 characters.", nameof(message));

            var email = (supportEmail ?? string.Empty).Trim();
            if (email.Length is < 5 or > 150 || !email.Contains('@', StringComparison.Ordinal))
                throw new ArgumentException("Support email is not configured correctly.", nameof(supportEmail));

            return new SupportRequest(
                societyId,
                residentId,
                cleanTopic,
                cleanMessage,
                TrimOrNull(residentName, 120),
                TrimOrNull(residentPhone, 20),
                TrimOrNull(towerWing, 40),
                TrimOrNull(flatNumber, 40),
                email,
                DateTime.UtcNow);
        }

        private static string? TrimOrNull(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }
    }
}
