using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class PackageDimensions : ValueObject
    {
        public double Weight { get; private set; }
        public double Height { get; private set; }
        public double Width { get; private set; }
        public double Length { get; private set; }

        
        private PackageDimensions(double weight, double height, double width, double length)
        {
            Weight = weight;
            Height = height;
            Width = width;
            Length = length;
        }
        public static PackageDimensions Create(double weight, double height, double width, double length)
        {
            if (weight <= 0 || height <= 0 || width <= 0 || length <= 0)
                throw new ArgumentException("Dimensions and weight must be greater than zero.");
            return new PackageDimensions(weight, height, width, length);

        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Weight;
            yield return Height;
            yield return Width;
            yield return Length;
        }
    }
}
