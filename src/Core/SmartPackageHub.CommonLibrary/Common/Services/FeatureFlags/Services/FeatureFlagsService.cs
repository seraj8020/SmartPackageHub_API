using SmartPackageHub.Common.Services.FeatureFlags.Abstractions;
using SmartPackageHub.Common.Services.FeatureFlags.Options;
using Microsoft.Extensions.Options;


namespace SmartPackageHub.Common.Services.FeatureFlags.Services
{
    public sealed class FeatureFlagsService : IFeatureFlags
    {
        private readonly IOptionsMonitor<FeatureFlagsOptions> _options;

        public FeatureFlagsService(IOptionsMonitor<FeatureFlagsOptions> options)
        {
            _options = options;
        }

        public bool CashOnDelivery => _options.CurrentValue.CashOnDelivery;

        public bool SmsOtp => _options.CurrentValue.SmsOtp;

        public bool IsEnabled(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
                return false;

            return featureName.Trim().ToUpperInvariant() switch
            {
                "CASHONDELIVERY" or "COD" or "CASH_ON_DELIVERY" => CashOnDelivery,
                "SMSOTP" or "SMS_OTP" or "LOGINOTP_SMS" or "SENDLOGINOTPSMS" => SmsOtp,
                _ => false
            };
        }

        public IReadOnlyDictionary<string, bool> GetAll() =>
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["CashOnDelivery"] = CashOnDelivery,
                ["SmsOtp"] = SmsOtp
            };
    }
}
