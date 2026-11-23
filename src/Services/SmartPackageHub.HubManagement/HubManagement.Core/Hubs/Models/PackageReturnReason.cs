namespace HubManagement.Core.Hubs.Models
{
    /// <summary>Why the resident is returning a delivered package.</summary>
    public enum PackageReturnReason
    {
        /// <summary>Not as expected / don’t like.</summary>
        NotAsExpected,

        /// <summary>Damaged inside (contents).</summary>
        DamagedInside,

        Expired,

        /// <summary>Mechanical / technical fault.</summary>
        TechnicalFault,

        Other,
    }
}
