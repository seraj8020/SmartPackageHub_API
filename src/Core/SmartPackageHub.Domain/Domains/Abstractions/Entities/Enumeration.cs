using System.Reflection;

namespace SmartPackageHub.Domains.Abstractions.Entities
{
#pragma warning disable CA1724 // Domain enumeration base type; it is not System.IO.Enumeration.
    public abstract class Enumeration : IComparable
    {
        public string Name { get; private set; } = string.Empty;

        public int Id { get; private set; }

        protected Enumeration()
        {
        }

        protected Enumeration(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => Name;

        public static IEnumerable<T> GetAll<T>() where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is not Enumeration otherValue)
            {
                return false;
            }

            var typeMatches = GetType() == otherValue.GetType();
            var valueMatches = Id.Equals(otherValue.Id);
            return typeMatches && valueMatches;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(Enumeration? left, Enumeration? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Enumeration? left, Enumeration? right) => !(left == right);

        public static bool operator <(Enumeration? left, Enumeration? right)
        {
            if (left is null || right is null)
            {
                return false;
            }

            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Enumeration? left, Enumeration? right)
        {
            if (left is null || right is null)
            {
                return false;
            }

            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Enumeration? left, Enumeration? right)
        {
            if (left is null || right is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Enumeration? left, Enumeration? right)
        {
            if (left is null || right is null)
            {
                return false;
            }

            return left.CompareTo(right) >= 0;
        }

        public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
        {
            ArgumentNullException.ThrowIfNull(firstValue);
            ArgumentNullException.ThrowIfNull(secondValue);
            return Math.Abs(firstValue.Id - secondValue.Id);
        }

        public static T FromValue<T>(int value) where T : Enumeration
        {
            var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
            return matchingItem;
        }

        public static T FromDisplayName<T>(string displayName) where T : Enumeration
        {
            var matchingItem = Parse<T, string>(displayName, "display name", item => string.Equals(item.Name, displayName, StringComparison.Ordinal));
            return matchingItem;
        }

        private static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);
            if (matchingItem == null)
            {
                throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
            }

            return matchingItem;
        }

        public int CompareTo(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return Id.CompareTo(((Enumeration)obj).Id);
        }
    }
#pragma warning restore CA1724
}
