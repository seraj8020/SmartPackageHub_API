using SmartPackageHub.Domains.Abstractions.ValueObjects;

namespace HubManagement.Core.Hubs.ValueObjects
{
    public sealed class AccessCode : ValueObject
    {
        public string TokenHash { get; private set; }

        private AccessCode(string tokenHash)
        {
            TokenHash = tokenHash;
        }
        public static AccessCode Create(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("Token hash cannot be empty.");
            return new AccessCode(tokenHash);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return TokenHash;
        }
    }
}
