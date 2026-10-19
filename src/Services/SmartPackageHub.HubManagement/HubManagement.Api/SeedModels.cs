public sealed class SocietySeed
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public sealed class UserSeed
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Resident";
    public string Status { get; set; } = "Active";
}

public sealed class UserSocietySeed
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SocietyId { get; set; }
}

public sealed class ResidentSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string TowerWing { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public bool IsMember { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MembershipSeed
{
    public Guid Id { get; set; }
    public Guid ResidentId { get; set; }
    public Guid SocietyId { get; set; }
    public string Tier { get; set; } = "Silver";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsAutoRenewEnabled { get; set; }
}

public sealed class ShelfSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TowerOrWing { get; set; } = string.Empty;
    public bool IsColdStorage { get; set; }
    public int Capacity { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public int Tier { get; set; }
}

public sealed class CourierAgentSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string CarrierName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public string? EmployeeOrBadgeNumber { get; set; }
}

public sealed class SecureBagSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string BagNumber { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public string? SealNumber { get; set; }
    public string? CapacityOrSize { get; set; }
    public string Status { get; set; } = "Assigned";
}

public sealed class DigitalSignOffReceiptSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string CourierPartner { get; set; } = string.Empty;
    public Guid? CourierAgentId { get; set; }
    public string CourierAgentName { get; set; } = string.Empty;
    public string CourierAgentPhone { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public int TotalPackagesCount { get; set; }
    public string? DigitalSignature { get; set; }
    public string Status { get; set; } = "Acknowledged";
    public DateTime SignedAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class PackageSeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? ShelfId { get; set; }
    public Guid? SecureBagId { get; set; }
    public Guid? DigitalSignOffReceiptId { get; set; }
    public Guid StaffUserId { get; set; }
    public string PkgNumber { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }
    public double Length { get; set; }
    public string? Category { get; set; }
    public string CourierPartner { get; set; } = string.Empty;
    public string CourierAwb { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string TowerWing { get; set; } = string.Empty;
    public string FlatNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Received";
    public bool IsColdStorage { get; set; }
    public bool IsCod { get; set; }
    public decimal? CodAmount { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public sealed class PackageIntakeSeed
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Guid? CourierAgentId { get; set; }
    public DateTime ScannedAt { get; set; }
    public string MatchStatus { get; set; } = "matched";
    public string? RawOcrText { get; set; }
    public int? OcrElapsedMs { get; set; }
    public string? ClientRequestId { get; set; }
    public string? Barcode { get; set; }
    public bool IsDamaged { get; set; }
    public string? DamageNote { get; set; }
}

public sealed class PackageImageSeed
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Guid IntakeId { get; set; }
    public string Kind { get; set; } = "label";
    public string StorageKey { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long ByteSize { get; set; }
    public DateTime CapturedAt { get; set; }
}

public sealed class AnalyticsDailySeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public DateTime Day { get; set; }
    public int PackagesReceived { get; set; }
    public int PackagesDelivered { get; set; }
    public int OverdueCount { get; set; }
}

public sealed class DeliverySeed
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid PackageId { get; set; }
    public Guid HandedByUserId { get; set; }
    public Guid ResidentId { get; set; }
    public string Method { get; set; } = "CounterPickup";
    public DateTime DeliveredAt { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public string SlotStatus { get; set; } = "booked";
    public string Status { get; set; } = "Completed";
    public string Notes { get; set; } = string.Empty;
}
