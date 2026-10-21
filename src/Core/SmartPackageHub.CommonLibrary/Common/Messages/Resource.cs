namespace SmartPackageHub.Common.Messages
{
    public class Resource
    {
        public string Service { get; }
        public string Endpoint { get; }

        protected Resource()
        {
            Service = string.Empty;
            Endpoint = string.Empty;
        }

        protected Resource(string service, string endpoint)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(endpoint);
#pragma warning disable CA1308 // Resource identifiers are matched in lowercase.
            Service = service.ToLowerInvariant();
            Endpoint = endpoint.ToLowerInvariant();
#pragma warning restore CA1308
        }

        public static Resource Create(string service, string endpoint)
            => new Resource(service, endpoint);
    }
}
