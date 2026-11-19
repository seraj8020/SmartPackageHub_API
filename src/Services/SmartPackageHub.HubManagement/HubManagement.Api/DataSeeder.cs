using HubManagement.Application.Auth.Services;
using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;
using HubManagement.Core.Hubs.ValueObjects;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub.Domains.Abstractions.Entities;
using SmartPackageHub.HubManagement.Infrastructure;
using HubManagement.Api.SeedDataModel;

namespace SmartPackageHub.HubManagement.Api;

/// <summary>
/// Applies JSON seed data. Only Users.json is shipped; societies and other entities
/// are created via the admin portal after platform.admin login.
/// </summary>
internal static class DataSeeder
{
    public static WebApplication Seed(this WebApplication webApp, IDictionary<string, List<object>> allData)
    {
        ArgumentNullException.ThrowIfNull(webApp);
        ArgumentNullException.ThrowIfNull(allData);
        using var serviceScope = webApp.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<HubDbContext>();
        context.Database.Migrate();

        var newData = (Dictionary<string, List<object>>)allData;
        var added = AddMissing(context, BuildUsers(Items<UserSeed>(newData, "Users")), context.Users, u => u.Id);

        if (added)
        {
            var count = context.SaveChanges();
            Console.WriteLine($"Seed data applied. {count} rows inserted.");
        }
        else
        {
            Console.WriteLine("Seed data already present. 0 rows inserted.");
        }

        EnsureStaffPasswords(context);
        EnsurePlatformAdminRole(context);
        EnsurePlatformAdminEmail(context);
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
            if (user.Role.Type is not (RoleType.Staff or RoleType.Admin or RoleType.PlatformAdmin or RoleType.Courier))
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

    /// <summary>
    /// Ensures seeded platform.admin uses Role = PlatformAdmin (not society Admin).
    /// </summary>
    private static void EnsurePlatformAdminRole(HubDbContext context)
    {
        var updated = false;
        foreach (var user in context.Users.ToList())
        {
            if (!string.Equals(user.Username, "platform.admin", StringComparison.OrdinalIgnoreCase))
                continue;
            if (user.Role.Type == RoleType.PlatformAdmin)
                continue;
            user.ChangeRole(Role.Create(RoleType.PlatformAdmin));
            updated = true;
        }

        if (updated)
        {
            context.SaveChanges();
            Console.WriteLine("platform.admin role set to PlatformAdmin.");
        }
    }

    /// <summary>
    /// Points platform.admin at a deliverable mailbox so forgot-password/username emails work with Zoho.
    /// </summary>
    private static void EnsurePlatformAdminEmail(HubDbContext context)
    {
        const string deliverableEmail = "majid.akhter@pharmarena-x.com";
        var updated = false;
        foreach (var user in context.Users.ToList())
        {
            if (!string.Equals(user.Username, "platform.admin", StringComparison.OrdinalIgnoreCase))
                continue;
            var current = user.ContactInfo?.Email;
            if (string.Equals(current, deliverableEmail, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(current) &&
                !current.EndsWith(".local", StringComparison.OrdinalIgnoreCase) &&
                !current.Contains("smartpackagehub.local", StringComparison.OrdinalIgnoreCase))
                continue;

            user.ChangeContact(ContactInfo.Create(deliverableEmail));
            updated = true;
        }

        if (updated)
        {
            context.SaveChanges();
            Console.WriteLine($"Platform admin email updated to {deliverableEmail}.");
        }
    }

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
        }

        return true;
    }

    private static void AssignId(EntityBase entity, Guid id)
    {
        entity.GetType().GetProperty(nameof(EntityBase.Id))!.SetValue(entity, id);
    }

    private static void PinId(HubDbContext context, object entity, Guid id)
    {
        var idProperty = context.Entry(entity).Property("Id");
        idProperty.CurrentValue = id;
        idProperty.IsTemporary = false;
    }
}
