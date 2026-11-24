namespace SmartPackageHub.Common.Services.Storage.Services
{
    /// <summary>
    /// Two separate object directories for intake photos:
    /// <list type="bullet">
    /// <item><description><c>arrival/</c> — normal parcel / arrival photo (every intake).</description></item>
    /// <item><description><c>damage/</c> — damage photo when staff marks damaged.</description></item>
    /// </list>
    /// Legacy writes used <c>parcel-blobs/</c>; reads still fall back there.
    /// </summary>
    public static class PhotoStoragePaths
    {
        public const string ArrivalDirectory = "arrival";
        public const string DamageDirectory = "damage";

        /// <summary>Pre-arrival-directory layout — still readable for older blobs.</summary>
        public const string LegacyArrivalDirectory = "parcel-blobs";

        public static string ArrivalBlob(string clientPhotoId) =>
            $"{ArrivalDirectory}/{clientPhotoId}.jpg";

        public static string ArrivalMeta(string clientPhotoId) =>
            $"{ArrivalDirectory}/{clientPhotoId}.jpg.meta.json";

        public static string ArrivalLink(string clientPhotoId) =>
            $"{ArrivalDirectory}/{clientPhotoId}.link";

        public static string LegacyArrivalBlob(string clientPhotoId) =>
            $"{LegacyArrivalDirectory}/{clientPhotoId}.jpg";

        public static string LegacyArrivalMeta(string clientPhotoId) =>
            $"{LegacyArrivalDirectory}/{clientPhotoId}.jpg.meta.json";

        public static string LegacyArrivalLink(string clientPhotoId) =>
            $"{LegacyArrivalDirectory}/{clientPhotoId}.link";

        public static string DamageBlob(Guid intakeId) =>
            $"{DamageDirectory}/{intakeId:N}.jpg";
    }
}
