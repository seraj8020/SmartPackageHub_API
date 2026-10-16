using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class PackageIntake : EntityBase
    {
        public Guid PackageId { get; private set; }
        public Guid? StaffUserId { get; private set; }
        public Guid? CourierAgentId { get; private set; }
        public DateTime ScannedAt { get; private set; } = DateTime.UtcNow;
        public string MatchStatus { get; private set; } = "missing";
        public string? RawOcrText { get; private set; }
        public int? OcrElapsedMs { get; private set; }
        [Required]
        public string? ClientRequestId { get; private set; }
        public string? Barcode { get; private set; }
        public bool IsDamaged { get; set; }
        public string? DamageNote { get; set; }
        public string? DamageImagePath { get; set; }

        private PackageIntake() { }
        private PackageIntake(Guid packageId, Guid? staffUserId, DateTime scannedAt, string matchStatus, string rawOcrText, int ocrElapsedTime, string clientRequestId)
        {
            this.PackageId = packageId;
            this.StaffUserId = staffUserId;
            this.ScannedAt = scannedAt;
            this.MatchStatus = matchStatus;
            this.RawOcrText = rawOcrText;
            this.OcrElapsedMs = ocrElapsedTime;
            this.ClientRequestId = clientRequestId;
        }

        public static PackageIntake Create(Guid packageId, Guid? staffUserId, DateTime scannedAt, string matchStatus, string rawOcrText, int ocrElapsedTime, string clientRequestId)
        {
            var packageIntake = new PackageIntake(packageId, staffUserId, scannedAt, matchStatus, rawOcrText, ocrElapsedTime, clientRequestId);
            return packageIntake;
        }

        public void RecordLabelScan(
            Guid? courierAgentId,
            DateTime scannedAt,
            string matchStatus,
            string? rawOcrText,
            int? ocrElapsedMs,
            string? clientRequestId,
            string? barcode,
            bool isDamaged = false,
            string? damageNote = null,
            string? damageImagePath = null)
        {
            CourierAgentId = courierAgentId;
            ScannedAt = scannedAt;
            MatchStatus = string.IsNullOrWhiteSpace(matchStatus) ? "missing" : matchStatus.Trim();
            RawOcrText = rawOcrText;
            OcrElapsedMs = ocrElapsedMs;
            ClientRequestId = clientRequestId;
            Barcode = barcode;
            IsDamaged = isDamaged;
            DamageNote = damageNote;
            DamageImagePath = damageImagePath;
        }
    }
}
