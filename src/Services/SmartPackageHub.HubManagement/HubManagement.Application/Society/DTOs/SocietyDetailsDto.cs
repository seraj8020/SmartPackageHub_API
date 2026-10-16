using HubManagement.Core.Hubs.Entities;

namespace HubManagement.Application.Societies.DTOs
{
    public sealed class SocietyDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }

        public static SocietyDetailsDto From(Society society)
        {
            var street = society.Address.Street;
            var city = society.Address.City;
            var state = society.Address.State;
            var zipCode = society.Address.ZipCode;
            var country = society.Address.Country;

            return new SocietyDetailsDto
            {
                Id = society.Id,
                Name = society.Name,
                Street = street,
                City = city,
                State = state,
                ZipCode = zipCode,
                Country = country,
                Address = string.Join(", ", new[] { street, city, state, zipCode, country }.Where(part => !string.IsNullOrWhiteSpace(part))),
                ContactEmail = society.ContactEmail,
                Phone = society.Phone
            };
        }
    }

    public sealed record SocietyCommandResult(bool Found, bool Succeeded, string Message, SocietyDetailsDto? Society);

    public sealed record DeleteCommandResult(bool Found);
}
