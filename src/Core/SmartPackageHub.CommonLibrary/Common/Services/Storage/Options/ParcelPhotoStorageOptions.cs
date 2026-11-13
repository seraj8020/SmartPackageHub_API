namespace SmartPackageHub.Common.Services.Storage.Options
{
    /// <summary>
    /// Parcel / damage photo storage. Use Provider=Local for laptop/dev;
    /// Provider=Gcs for QA/UAT/Prod on Google Cloud Storage.
    /// </summary>
    public sealed class ParcelPhotoStorageOptions
    {
        public const string SectionName = "ParcelPhotoStorage";

        /// <summary>Local or Gcs (case-insensitive).</summary>
        public string Provider { get; set; } = "Local";

        /// <summary>GCS bucket name when Provider=Gcs.</summary>
        public string Bucket { get; set; } = string.Empty;

        /// <summary>
        /// Optional object key prefix, e.g. "qa/" so one bucket can host multiple envs.
        /// </summary>
        public string ObjectPrefix { get; set; } = string.Empty;

        public bool IsGcs =>
            string.Equals(Provider, "Gcs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Provider, "GCS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Provider, "GoogleCloudStorage", StringComparison.OrdinalIgnoreCase);
    }
}
