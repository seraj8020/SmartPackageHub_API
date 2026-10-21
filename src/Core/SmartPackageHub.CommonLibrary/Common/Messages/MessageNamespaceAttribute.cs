namespace SmartPackageHub.Common.Messages
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessageNamespaceAttribute : Attribute
    {
        public string Namespace { get; }

        public MessageNamespaceAttribute(string @namespace)
        {
            ArgumentNullException.ThrowIfNull(@namespace);
#pragma warning disable CA1308 // Message namespaces are matched in lowercase.
            Namespace = @namespace.ToLowerInvariant();
#pragma warning restore CA1308
        }
    }
}
