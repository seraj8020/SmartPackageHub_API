using Microsoft.AspNetCore.Http;

namespace SmartPackageHub.Domains.Abstractions.Guards
{
    public class BadRequestException : CustomException
    {
        public BadRequestException()
            : this("Bad request")
        {
        }

        public BadRequestException(string message)
            : base(message, StatusCodes.Status400BadRequest, null)
        {
        }

        public BadRequestException(string message, Exception innerException)
            : base(message, StatusCodes.Status400BadRequest, innerException)
        {
        }

        protected BadRequestException(string message, int statusCode, Exception? innerException, params string[] errors)
            : base(message, statusCode, innerException, errors)
        {
        }
    }
}
