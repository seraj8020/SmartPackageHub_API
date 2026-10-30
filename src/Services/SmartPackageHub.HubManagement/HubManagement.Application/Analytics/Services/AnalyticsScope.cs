namespace HubManagement.Application.Analytics.Services
{
    /// <summary>
    /// Optional society filter for admin analytics.
    /// Null / empty = all societies (platform admin).
    /// </summary>
    public static class AnalyticsScope
    {
        public static bool Includes(Guid? societyIdFilter, Guid societyId) =>
            societyIdFilter is null || societyIdFilter == Guid.Empty || societyIdFilter == societyId;

        public static IEnumerable<T> FilterBySociety<T>(
            IEnumerable<T> source,
            Guid? societyIdFilter,
            Func<T, Guid> societyIdSelector)
        {
            if (societyIdFilter is null || societyIdFilter == Guid.Empty)
                return source;
            return source.Where(item => societyIdSelector(item) == societyIdFilter.Value);
        }

        public static DateTime StartOfDayUtc(DateOnly day) =>
            DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        public static DateTime EndOfDayUtc(DateOnly day) =>
            DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(23, 59, 59, 999)), DateTimeKind.Utc);

        public static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
