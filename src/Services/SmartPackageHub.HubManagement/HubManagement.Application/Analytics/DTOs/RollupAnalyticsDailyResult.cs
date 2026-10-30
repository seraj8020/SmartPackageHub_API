namespace HubManagement.Application.Analytics.DTOs
{
    public sealed record RollupAnalyticsDailyResult(
        DateOnly Day,
        int SocietiesProcessed,
        int RowsUpserted,
        string Message);
}
