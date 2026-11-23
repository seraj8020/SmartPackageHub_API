namespace HubManagement.Application.Couriers.DTOs
{
    public sealed class CourierListResult<T>
    {
        public bool Forbidden { get; init; }
        public string? Message { get; init; }
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    }
}