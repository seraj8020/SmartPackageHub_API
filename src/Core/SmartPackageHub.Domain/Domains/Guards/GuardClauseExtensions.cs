using Microsoft.Extensions.Logging;

namespace SmartPackageHub.Domains.Abstractions.Guards
{
    public static partial class GuardClauseExtensions
    {
        private static ILogger? _logger;

        private static readonly Action<ILogger, string, Exception?> LogGuardError =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(Error)), "Error: {Event}");

        private static readonly Action<ILogger, string, Exception?> LogGuardNotFound =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(NotFound)), "Not found: {Event}");

        public static void SetLogger(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        private static void Error(string message)
        {
            if (_logger is not null)
            {
                LogGuardError(_logger, message, null);
            }

            throw new DomainException(message);
        }

        private static void NotFound(string message)
        {
            if (_logger is not null)
            {
                LogGuardNotFound(_logger, message, null);
            }

            throw new NotFoundException(message);
        }
    }
}
