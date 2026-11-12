namespace HubManagement.Application.Intake.DTOs
{
    /// <summary>
    /// Offline staff sync payload: multiple scanned packages flushed in one request.
    /// Each item uses the same rules as POST /api/v2/intake/confirm
    /// (including clientRequestId idempotency).
    /// </summary>
    public sealed class BatchIntakeRequest
    {
        public IList<ConfirmIntakeRequest> Items { get; } = new List<ConfirmIntakeRequest>();
    }
}
