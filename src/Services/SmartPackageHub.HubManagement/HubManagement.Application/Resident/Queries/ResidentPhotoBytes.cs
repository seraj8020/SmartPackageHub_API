namespace HubManagement.Application.HubResidents.Queries
{
    public sealed class ResidentPhotoBytes
    {
        public ResidentPhotoBytes(ReadOnlyMemory<byte> content, string contentType)
        {
            Content = content;
            ContentType = contentType;
        }

        public ReadOnlyMemory<byte> Content { get; }
        public string ContentType { get; }
    }
}
