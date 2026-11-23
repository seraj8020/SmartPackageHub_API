namespace HubManagement.Application.Courier.Services
{
    internal static class CourierAdminAccess
    {
        public static bool IsPlatformAdmin(string role) =>
            string.Equals(role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);

        public static bool CanAccessSociety(string role, IReadOnlyList<Guid> societyIds, Guid societyId)
        {
            if (IsPlatformAdmin(role))
                return true;
            return societyIds.Contains(societyId);
        }

        public static (Guid? FilterSocietyId, bool Forbidden, string? Message) TryResolveSocietyFilter(
            string role,
            IReadOnlyList<Guid> societyIds,
            Guid? requestedSocietyId)
        {
            if (IsPlatformAdmin(role))
            {
                var filter = requestedSocietyId is Guid id && id != Guid.Empty ? id : (Guid?)null;
                return (filter, false, null);
            }

            if (societyIds.Count == 0)
                return (null, true, "No society is linked to this admin.");

            if (requestedSocietyId is Guid asked && asked != Guid.Empty && !societyIds.Contains(asked))
                return (null, true, "You cannot view couriers for that society.");

            var filterSocietyId = requestedSocietyId is Guid value && value != Guid.Empty
                ? value
                : societyIds[0];
            return (filterSocietyId, false, null);
        }

        public static (Guid SocietyId, bool Forbidden, bool BadRequest, string? Message) TryResolveSocietyWrite(
            string role,
            IReadOnlyList<Guid> societyIds,
            Guid? requestedSocietyId)
        {
            if (IsPlatformAdmin(role))
            {
                if (requestedSocietyId is null || requestedSocietyId == Guid.Empty)
                    return (Guid.Empty, false, true, "Select a society.");
                return (requestedSocietyId.Value, false, false, null);
            }

            if (societyIds.Count == 0)
                return (Guid.Empty, true, false, "No society is linked to this admin.");

            if (requestedSocietyId is Guid asked && asked != Guid.Empty && !societyIds.Contains(asked))
                return (Guid.Empty, true, false, "You cannot manage couriers for that society.");

            var societyId = requestedSocietyId is Guid value && value != Guid.Empty
                ? value
                : societyIds[0];
            return (societyId, false, false, null);
        }
    }
}