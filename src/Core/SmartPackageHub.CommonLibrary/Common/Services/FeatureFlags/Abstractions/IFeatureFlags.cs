namespace SmartPackageHub.Common.Services.FeatureFlags.Abstractions
{
    public interface IFeatureFlags
    {
        bool CashOnDelivery { get; }

        /// <summary>When false, login OTP is not sent through the SMS provider.</summary>
        bool SmsOtp { get; }

        /// <summary>Lookup by feature name (case-insensitive). Unknown names return false.</summary>
        bool IsEnabled(string featureName);

        /// <summary>Snapshot of all known flags for the features API.</summary>
        IReadOnlyDictionary<string, bool> GetAll();
    }
}
