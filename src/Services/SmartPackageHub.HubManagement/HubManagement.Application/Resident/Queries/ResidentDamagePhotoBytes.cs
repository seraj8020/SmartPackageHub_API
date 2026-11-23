namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class ResidentDamagePhotoBytes
    {
        public ResidentDamagePhotoBytes(ReadOnlyMemory<byte> content, string contentType)
        {
            Content = content;
            ContentType = contentType;
        }

        public ReadOnlyMemory<byte> Content { get; }
        public string ContentType { get; }
    }
}
