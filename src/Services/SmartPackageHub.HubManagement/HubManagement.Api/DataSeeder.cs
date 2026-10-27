using System.Reflection;
using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Domains.Abstractions.Entities;
using SmartPackageHub.Domains.Abstractions.Models;
using SmartPackageHub.HubManagement.Infrastructure;
using HubManagement.Api.SeedDataModel;

namespace SmartPackageHub.HubManagement.Api;
internal static class DataSeeder
{
    public static WebApplication Seed(this WebApplication webApp,  IDictionary<string, List<object>> allData)
    {
        ArgumentNullException.ThrowIfNull(webApp);
        ArgumentNullException.ThrowIfNull(allData);
        using var serviceScope = webApp.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<HubDbContext>();
        context.Database.Migrate();
        Dictionary<string, List<object>> newData = (Dictionary<string, List<object>>)allData;
        var intakes = Items<PackageIntakeSeed>(newData, "PackageIntakes");
        var added = false;
        added |= AddMissing(context, BuildSocieties(Items<SocietySeed>(newData, "Societies")), context.Societies, s => s.Id);
        added |= AddMissing(context, BuildUsers(Items<UserSeed>(newData, "Users")), context.Users, u => u.Id);
        added |= AddMissing(context, BuildUserSocieties(Items<UserSocietySeed>(newData, "UserSocieties")), context.UsersSocieties, link => link.Id);
        added |= AddMissing(context, BuildResidents(Items<ResidentSeed>(newData, "Residents")), context.Residents, r => r.Id);
        added |= AddMissing(context, BuildMemberships(Items<MembershipSeed>(newData, "Memberships")), context.Memberships, m => m.Id);
        added |= AddMissing(context, BuildShelves(Items<ShelfSeed>(newData, "Shelves")), context.Shelves, s => s.Id);
        added |= AddMissing(context, BuildCourierAgents(Items<CourierAgentSeed>(newData, "CourierAgents")), context.CourierAgents, a => a.Id);
        added |= AddMissing(context, BuildSecureBags(Items<SecureBagSeed>(newData, "SecureBags")), context.SecureBags, b => b.Id);
        added |= AddMissing(context, BuildReceipts(Items<DigitalSignOffReceiptSeed>(newData, "DigitalSignOffReceipts")), context.DigitalSignOffReceipts, r => r.Id);
        added |= AddMissing(context, BuildPackages(Items<PackageSeed>(newData, "Packages"), intakes), context.Packages, p => p.Id);
        added |= AddMissing(context, BuildImages(Items<PackageImageSeed>(newData, "PackageImages")), context.PackageImages, i => i.Id);
        added |= AddMissing(context, BuildAnalytics(Items<AnalyticsDailySeed>(newData, "AnalyticsDailies")), context.AnalyticsDailies, a => a.Id);
        added |= AddMissing(context, BuildDeliveries(Items<DeliverySeed>(newData, "Deliveries")), context.Deliveries, d => d.Id);
        added |= AddMissing(context, BuildDeliverySlots(Items<DeliverySlotSeed>(newData, "DeliverySlots")), context.DeliverySlots, s => s.Id);

        if (added)
        {
            var count = context.SaveChanges();
            Console.WriteLine($"Seed data applied. {count} rows inserted.");
        }
        else
        {
            var count = 0;
            Console.WriteLine($"Seed data already present.{count} rows inserted.");
        }

        EnsureStaffPasswords(context);
        return webApp;
    }

    /// <summary>
    /// Seeds a known local password for staff/admin users that have no PasswordHash yet.
    /// Default: Staff@123 (change in production).
    /// </summary>
    private static void EnsureStaffPasswords(HubDbContext context)
    {
        const string defaultPassword = "Staff@123";
        var updated = false;
        foreach (var user in context.Users.ToList())
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                continue;
            if (user.Role.Type is not (RoleType.Staff or RoleType.Admin or RoleType.Courier))
                continue;
            user.SetPasswordHash(PasswordHasher.Hash(defaultPassword));
            updated = true;
        }

        if (updated)
        {
            context.SaveChanges();
            Console.WriteLine("Staff/admin seed passwords applied (default Staff@123).");
        }
    }

    private static List<Society> BuildSocieties(List<SocietySeed> seeds) =>
        seeds.Select(seed =>
        {
            var society = Society.Create(
                seed.Name,
                Address.Create(new AddressData(seed.Street, seed.City, seed.State, seed.ZipCode, seed.Country)),
                seed.ContactEmail,
                seed.Phone);
            AssignId(society, seed.Id);
            return society;
        }).ToList();

    private static List<User> BuildUsers(List<UserSeed> seeds) =>
        seeds.Select(seed =>
        {
            var user = User.Create(
                seed.Username,
                ContactInfo.Create(seed.Email),
                Role.Create(Enum.Parse<RoleType>(seed.Role, ignoreCase: true)),
                seed.Status,
                phoneNumber: seed.PhoneNumber);
            AssignId(user, seed.Id);
            return user;
        }).ToList();

    private static List<UserSociety> BuildUserSocieties(List<UserSocietySeed> seeds) =>
        seeds.Select(seed =>
        {
            var link = UserSociety.Create(seed.UserId, seed.SocietyId);
            AssignId(link, seed.Id);
            return link;
        }).ToList();
   
    private static List<Resident> BuildResidents(List<ResidentSeed> seeds) =>
    seeds.Select(seed =>
    {
        // Safely parse the URL string to a System.Uri object
        Uri? avatarUri = Uri.TryCreate(seed.AvatarUrl, UriKind.Absolute, out var parsedUri)
            ? parsedUri
            : null;

        var resident = Resident.Create(
            seed.SocietyId,
            seed.UserId,
            seed.FullName,
            seed.PhoneNumber,
            seed.TowerWing,
            seed.UnitNumber,
            seed.IsMember,
            avatarUri, // Now passing the Uri object instead of a string
            seed.IsActive);

        AssignId(resident, seed.Id);
        return resident;
    }).ToList();
    private static List<Membership> BuildMemberships(List<MembershipSeed> seeds) =>
        seeds.Select(seed =>
        {
            var membership = Membership.Create(
                seed.ResidentId,
                seed.SocietyId,
                SubscriptionTier.Create(seed.Tier),
                ValidityPeriod.Create(seed.StartDate, seed.EndDate),
                seed.Status,
                seed.IsAutoRenewEnabled);
            AssignId(membership, seed.Id);
            return membership;
        }).ToList();

    private static List<Shelf> BuildShelves(List<ShelfSeed> seeds) =>
        seeds.Select(seed =>
        {
            var shelf = Shelf.Create(
                seed.SocietyId,
                seed.Code,
                seed.TowerOrWing,
                seed.IsColdStorage,
                seed.Capacity,
                ShelfLocation.Create(seed.Zone, seed.Aisle, seed.Tier));
            AssignId(shelf, seed.Id);
            return shelf;
        }).ToList();

    private static List<CourierAgent> BuildCourierAgents(List<CourierAgentSeed> seeds) =>
        seeds.Select(seed =>
        {
            var agent = CourierAgent.Create(
                seed.SocietyId,
                seed.CarrierName,
                seed.AgentName,
                seed.Phone,
                seed.VehicleNumber,
                seed.EmployeeOrBadgeNumber);
            AssignId(agent, seed.Id);
            return agent;
        }).ToList();

    private static List<SecureBag> BuildSecureBags(List<SecureBagSeed> seeds) =>
        seeds.Select(seed =>
        {
            var bag = SecureBag.Create(
                seed.SocietyId,
                seed.BagNumber,
                seed.Barcode,
                seed.QrCode,
                seed.SealNumber,
                seed.CapacityOrSize,
                seed.Status);
            AssignId(bag, seed.Id);
            return bag;
        }).ToList();

    private static List<DigitalSignOffReceipt> BuildReceipts(List<DigitalSignOffReceiptSeed> seeds) =>
        seeds.Select(seed =>
        {
            var receipt = DigitalSignOffReceipt.Create(
                seed.SocietyId,
                seed.ReceiptNumber,
                seed.CourierPartner,
                seed.CourierAgentName,
                seed.CourierAgentPhone,
                seed.StaffUserId,
                seed.TotalPackagesCount,
                seed.CourierAgentId,
                seed.DigitalSignature,
                seed.Status,
                seed.SignedAt,
                seed.Notes);
            AssignId(receipt, seed.Id);
            return receipt;
        }).ToList();

    private static List<Package> BuildPackages(List<PackageSeed> seeds, List<PackageIntakeSeed> intakes) =>
        seeds.Select(seed =>
        {
            var package = Package.Create(
                seed.SocietyId,
                seed.ResidentId,
                seed.ShelfId,
                seed.StaffUserId,
                seed.TrackingNumber,
                PackageDimensions.Create(seed.Weight, seed.Height, seed.Width, seed.Length),
                seed.CourierPartner,
                seed.CourierAwb,
                seed.RecipientName,
                seed.TowerWing,
                seed.FlatNumber,
                Enum.Parse<PackageStatus>(seed.Status, ignoreCase: true),
                seed.ReceivedAt,
                seed.IsColdStorage,
                seed.IsCod,
                seed.CodAmount,
                null,
                seed.PkgNumber,
                seed.SecureBagId,
                seed.DigitalSignOffReceiptId,
                seed.Category);
            AssignId(package, seed.Id);

            var intake = package.PackageIntake;
            AssignProperty(intake, nameof(PackageIntake.PackageId), seed.Id);
            var intakeSeed = intakes.FirstOrDefault(item => item.PackageId == seed.Id);
            if (intakeSeed != null)
            {
                AssignId(intake, intakeSeed.Id);
                intake.RecordLabelScan(
                    intakeSeed.CourierAgentId,
                    intakeSeed.ScannedAt,
                    intakeSeed.MatchStatus,
                    intakeSeed.RawOcrText,
                    intakeSeed.OcrElapsedMs,
                    intakeSeed.ClientRequestId,
                    intakeSeed.Barcode,
                    intakeSeed.IsDamaged,
                    intakeSeed.DamageNote);
            }

            return package;
        }).ToList();

    private static List<PackageImage> BuildImages(List<PackageImageSeed> seeds) =>
        seeds.Select(seed =>
        {
            var image = PackageImage.Create(seed.PackageId, seed.IntakeId, seed.Kind, seed.StorageKey, seed.Sha256Hash, seed.CapturedAt);
            image.ContentType = seed.ContentType;
            image.ByteSize = seed.ByteSize;
            AssignId(image, seed.Id);
            return image;
        }).ToList();

    private static List<AnalyticsDaily> BuildAnalytics(List<AnalyticsDailySeed> seeds) =>
        seeds.Select(seed =>
        {
            var daily = AnalyticsDaily.Create(
                seed.SocietyId,
                DateOnly.FromDateTime(seed.Day),
                seed.PackagesReceived,
                seed.PackagesDelivered,
                seed.OverdueCount);
            AssignId(daily, seed.Id);
            return daily;
        }).ToList();

    private static List<Delivery> BuildDeliveries(List<DeliverySeed> seeds) =>
        seeds.Select(seed =>
        {
            var delivery = Delivery.Create(
                seed.SocietyId,
                seed.PackageId,
                seed.HandedByUserId,
                seed.ResidentId,
                null,
                null,
                seed.Method,
                seed.DeliveredAt,
                seed.Notes);
            if (seed.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                delivery.CompleteDelivery();
            else if (seed.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                delivery.RejectDelivery();
            AssignId(delivery, seed.Id);
            return delivery;
        }).ToList();

    private static List<DeliverySlot> BuildDeliverySlots(List<DeliverySlotSeed> seeds) =>
        seeds.Select(seed =>
        {
            var slot = DeliverySlot.Create(
                seed.SocietyId,
                seed.ResidentId,
                seed.PackageId,
                seed.WindowStart,
                seed.WindowEnd,
                seed.Status,
                seed.Notes);
            AssignId(slot, seed.Id);
            return slot;
        }).ToList();

    private static List<TEntity> Items<TEntity>(Dictionary<string, List<object>> allData, string key)
        where TEntity : class
    {
        if (!allData.TryGetValue(key, out var data) || data.Count == 0)
            return new List<TEntity>();

        return data.Cast<TEntity>().ToList();
    }

    private static bool AddMissing<TEntity>(
        HubDbContext context,
        List<TEntity> entities,
        DbSet<TEntity> set,
        Func<TEntity, Guid> idSelector) where TEntity : class
    {
        if (entities.Count == 0)
            return false;

        var ids = entities.Select(idSelector).ToList();
        var existingIds = set
            .Where(e => ids.Contains(EF.Property<Guid>(e, "Id")))
            .Select(e => EF.Property<Guid>(e, "Id"))
            .ToHashSet();

        var missing = entities.Where(e => !existingIds.Contains(idSelector(e))).ToList();
        if (missing.Count == 0)
            return false;

        foreach (var entity in missing)
        {
            set.Add(entity);
            PinId(context, entity, idSelector(entity));
            if (entity is Package package)
                PinId(context, package.PackageIntake, package.PackageIntake.Id);
        }

        return true;
    }

    private static void AssignId(EntityBase entity, Guid id)
    {
        entity.GetType().GetProperty(nameof(EntityBase.Id))!.SetValue(entity, id);
    }

    private static void AssignProperty(object entity, string propertyName, object? value)
    {
        entity.GetType().GetProperty(propertyName)!.SetValue(entity, value);
    }

    private static void PinId(HubDbContext context, object entity, Guid id)
    {
        var idProperty = context.Entry(entity).Property("Id");
        idProperty.CurrentValue = id;
        idProperty.IsTemporary = false;
    }
}
