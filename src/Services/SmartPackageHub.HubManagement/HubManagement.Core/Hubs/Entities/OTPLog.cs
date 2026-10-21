using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class OtpLog : EntityBase, IAggregateRoot
    {
        public Guid SocietyId { get; set; }
        public Guid ResidentId { get; set; }
        public Guid PackageId { get; private set; }
        public OtpToken? Token { get; private set; }
        public ExpiryTime? Expiry { get; private set; }
        public string? Purpose { get; private set; }
        public bool IsUsed { get; private set; }

        private OtpLog() { }
        private OtpLog(Guid societyId, Guid residentId, Guid packageId, OtpToken token, ExpiryTime expiry, string purpose)
        {
            SocietyId = societyId;
            ResidentId = residentId;
            PackageId = packageId;
            Token = token;
            Expiry = expiry;
            Purpose = purpose;
            IsUsed = false;
        }

        public static OtpLog Generate(Guid societyId, Guid residentId, Guid packageId, string rawToken, TimeSpan validityDuration, string purpose)
        {
            if (packageId == Guid.Empty)
                throw new ArgumentNullException(nameof(packageId));
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentNullException(nameof(rawToken));
            var token = OtpToken.Create(rawToken);
            var expiry = ExpiryTime.Create(DateTime.UtcNow.Add(validityDuration));
            return new OtpLog(societyId, residentId, packageId, token, expiry, purpose);
        }

        // Domain Behaviors
        public bool ValidateCollection(string inputToken)
        {
            if (IsUsed) return false;
            if (Expiry!.IsExpired) return false;

            if (string.Equals(Token!.Value, inputToken, StringComparison.Ordinal))
            {
                IsUsed = true; // State change happens securely within the aggregate root boundary
                return true;
            }

            return false;
        }
    }
}
