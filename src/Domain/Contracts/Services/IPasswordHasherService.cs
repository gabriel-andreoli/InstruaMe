namespace InstruaMe.Domain.Contracts.Services
{
    public interface IPasswordHasherService
    {
        (string hash, string salt) Hash(string password);
        bool Verify(string password, string hash, string salt);
    }
}
