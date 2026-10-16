
namespace HubManagement.Application.Societies.DTOs
{
    public class SocietyDTO
    {
        public string Name { get; set; } = null!;
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; private set; }
        
    }
}
