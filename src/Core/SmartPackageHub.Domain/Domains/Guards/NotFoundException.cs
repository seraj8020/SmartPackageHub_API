
namespace SmartPackageHub.Domains.Abstractions.Guards
{
    public sealed class NotFoundException : Exception
    {
        public NotFoundException() : this("Not found")
        {

        }

        public NotFoundException(string message) : base(message)
        {

        }
    }
}
