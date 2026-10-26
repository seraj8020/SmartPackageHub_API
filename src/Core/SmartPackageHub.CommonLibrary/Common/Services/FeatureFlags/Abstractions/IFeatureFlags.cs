namespace SmartPackageHub.Common.Services.FeatureFlags.Abstractions
{
    public interface IFeatureFlags
    {
        bool CashOnDelivery { get; }

        /// <summary>Lookup by feature name (case-insensitive). Unknown names return false.</summary>
        bool IsEnabled(string featureName);

        /// <summary>Snapshot of all known flags for the features API.</summary>
        IReadOnlyDictionary<string, bool> GetAll();
    }
}
