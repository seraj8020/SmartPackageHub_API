namespace SmartPackageHub_API.Services
{
    public interface ITokenService
    {
        string CreateToken(Guid residentId, string name);
    }
}
