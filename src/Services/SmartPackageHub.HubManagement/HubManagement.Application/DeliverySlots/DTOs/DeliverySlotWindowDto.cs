namespace HubManagement.Application.DeliverySlots.DTOs
{
    public sealed class DeliverySlotWindowDto
    {
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int BookedCount { get; set; }
        public int Remaining { get; set; }
        public bool IsFull { get; set; }
        /// <summary>
        /// True for the system 6–9 fallback already assigned on shelving.
        /// Residents cannot book this window; they pick 9–12 / 12–3 / 3–6 to change it.
        /// </summary>
        public bool IsDefaultWindow { get; set; }
    }
}
