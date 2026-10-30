namespace HubManagement.Application.Societies.DTOs
{
    public sealed class SocietyShelfDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string TowerOrWing { get; set; } = string.Empty;
        public bool IsColdStorage { get; set; }
        public int Capacity { get; set; }
    }
}
