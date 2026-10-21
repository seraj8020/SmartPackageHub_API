using Microsoft.AspNetCore.Http;

namespace SmartPackageHub.Domains.Abstractions.Guards
{
    public class CustomException : Exception
    {
        public CustomException()
            : this("An error occurred.", StatusCodes.Status500InternalServerError, null)
        {
        }

        public CustomException(string message)
            : this(message, StatusCodes.Status500InternalServerError, null)
        {
        }

        public CustomException(string message, Exception innerException)
            : this(message, StatusCodes.Status500InternalServerError, innerException)
        {
        }

        protected CustomException(string message, int statusCode, Exception? innerException, params string[] errors)
            : base(message, innerException)
        {
            ErrorMessages = errors;
            StatusCode = statusCode;
        }

        public IEnumerable<string> ErrorMessages { get; protected set; }

        public int StatusCode { get; protected set; }

        public override string ToString()
        {
            return GetType().FullName ?? GetType().Name;
        }
    }
}
