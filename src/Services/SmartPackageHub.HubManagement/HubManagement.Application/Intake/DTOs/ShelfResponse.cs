namespace HubManagement.Application.Intake.DTOs
{
    public sealed class ShelfResponse
    {
        public Guid? ShelfId { get; set; }
        public Guid? SocietyId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
    }
}
