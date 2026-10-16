using Microsoft.Extensions.Logging;

namespace SmartPackageHub.Domains.Abstractions.Guards
{
    public static partial class GuardClauseExtensions
    {
        private static ILogger _logger;
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
        private static void Error(string message)
        {
            _logger.LogError("Error: {@Event}", message);
            throw new DomainException(message);
        }

        private static void NotFound(string message)
        {
            _logger.LogInformation("Not Found: {@Event}", message);
            throw new NotFoundException(message);
        }
    }
}
