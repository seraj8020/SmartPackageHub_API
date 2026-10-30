namespace SmartPackageHub.Common.ConfigOptions
{
    /// <summary>
    /// Auth behaviour toggles. Used with FeatureFlags:SmsOtp for cheap local/dev login.
    /// </summary>
    public sealed class AuthOptions
    {
        public const string SectionName = "Auth";

        /// <summary>
        /// When FeatureFlags:SmsOtp is false, this fixed 6-digit OTP is accepted for resident login
        /// (instead of a random SMS code). Leave empty to generate a random OTP still returned in the API.
        /// </summary>
        public string? FixedLoginOtp { get; set; } = "123456";
    }
}
