
namespace SmartPackageHub.Domains.Extensions
{
    public record NotNegativeOrZero
    {
        public NotNegativeOrZero(int value) => Value = value.NotBeNegativeOrZero();
        public int Value { get; }
    }
}
