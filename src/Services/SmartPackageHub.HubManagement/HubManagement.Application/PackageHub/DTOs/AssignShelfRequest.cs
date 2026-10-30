namespace HubManagement.Application.PackageHub.DTOs
{
    public sealed class AssignShelfRequest
    {
        public string? ShelfCode { get; set; }
        public bool IsColdStorage { get; set; }
    }
}
