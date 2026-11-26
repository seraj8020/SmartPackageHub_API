using System.Security.Cryptography;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    /// <summary>Resident in-app support ticket (Traizr-style list + new request).</summary>
    public sealed class SupportRequest : EntityBase, IAggregateRoot
    {
        private static readonly char[] ReferenceAlphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        public Guid SocietyId { get; private set; }
        public Guid ResidentId { get; private set; }
        /// <summary>Short public reference (e.g. VVRED9U).</summary>
        public string ReferenceCode { get; private set; } = string.Empty;
        /// <summary>Ticket subject line.</summary>
        public string Subject { get; private set; } = string.Empty;
        /// <summary>Category label (mapped from client category / topic).</summary>
        public string Topic { get; private set; } = string.Empty;
        public string Urgency { get; private set; } = "Normal";
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
            string referenceCode,
            string subject,
            string topic,
            string urgency,
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
            ReferenceCode = referenceCode;
            Subject = subject;
            Topic = topic;
            Urgency = urgency;
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
            string subject,
            string category,
            string urgency,
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

            var cleanSubject = (subject ?? string.Empty).Trim();
            if (cleanSubject.Length is < 2 or > 120)
                throw new ArgumentException("Subject must be between 2 and 120 characters.", nameof(subject));

            var cleanCategory = (category ?? string.Empty).Trim();
            if (cleanCategory.Length is < 2 or > 80)
                throw new ArgumentException("Category must be between 2 and 80 characters.", nameof(category));

            var cleanUrgency = NormalizeUrgency(urgency);

            var cleanMessage = (message ?? string.Empty).Trim();
            if (cleanMessage.Length is < 8 or > 4000)
                throw new ArgumentException("Message must be between 8 and 4000 characters.", nameof(message));

            var email = (supportEmail ?? string.Empty).Trim();
            if (email.Length is < 5 or > 150 || !email.Contains('@', StringComparison.Ordinal))
                throw new ArgumentException("Support email is not configured correctly.", nameof(supportEmail));

            return new SupportRequest(
                societyId,
                residentId,
                NewReferenceCode(),
                cleanSubject,
                cleanCategory,
                cleanUrgency,
                cleanMessage,
                TrimOrNull(residentName, 120),
                TrimOrNull(residentPhone, 20),
                TrimOrNull(towerWing, 40),
                TrimOrNull(flatNumber, 40),
                email,
                DateTime.UtcNow);
        }

        private static string NormalizeUrgency(string? urgency)
        {
            var value = (urgency ?? string.Empty).Trim();
            if (value.Length == 0)
                return "Normal";
            if (value.Equals("Low", StringComparison.OrdinalIgnoreCase))
                return "Low";
            if (value.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Urgent", StringComparison.OrdinalIgnoreCase))
                return "High";
            return "Normal";
        }

        private static string NewReferenceCode()
        {
            Span<char> chars = stackalloc char[7];
            Span<byte> bytes = stackalloc byte[7];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < chars.Length; i++)
                chars[i] = ReferenceAlphabet[bytes[i] % ReferenceAlphabet.Length];
            return new string(chars);
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
