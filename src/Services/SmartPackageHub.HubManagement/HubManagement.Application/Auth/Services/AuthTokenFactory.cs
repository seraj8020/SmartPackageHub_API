using System.Security.Cryptography;

namespace HubManagement.Application.Auth.Services
{
    public static class AuthTokenFactory
    {
        public static string CreateOpaqueToken() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
