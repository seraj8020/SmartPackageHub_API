namespace HubManagement.Application.Intake
{
    public sealed record StoredParcelPhoto(string ClientPhotoId, string StorageKey, string Sha256, string ContentType, long ByteSize);
}
