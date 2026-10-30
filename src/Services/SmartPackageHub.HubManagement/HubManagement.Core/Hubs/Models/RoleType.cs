namespace HubManagement.Core.Hubs.Models
{
    public enum RoleType
    {
        /// <summary>All-society ops admin (ops portal).</summary>
        PlatformAdmin,
        /// <summary>Society-scoped admin.</summary>
        Admin,
        Courier,
        Resident,
        Staff
    }
}
