namespace SmartPackageHub.Common.ConfigOptions
{
    public sealed class SmsPumpingProtectionOptions
    {
        public const string SectionName = "SmsPumpingProtection";

        /// <summary>Only India (+91) mobiles are accepted for OTP SMS.</summary>
        public bool RestrictToIndia { get; set; } = true;

        /// <summary>Max OTP SMS requests per client IP per window.</summary>
        public int MaxRequestsPerIp { get; set; } = 5;

        public int IpWindowMinutes { get; set; } = 15;

        /// <summary>Max OTP SMS per phone number per window.</summary>
        public int MaxRequestsPerPhone { get; set; } = 3;

        public int PhoneWindowMinutes { get; set; } = 30;

        public bool RecaptchaRequired { get; set; }

        public string? RecaptchaSecretKey { get; set; }

        public double RecaptchaMinScore { get; set; } = 0.5;
    }
}
