
namespace Votaciones.Application.Interfaces.ISecurity
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string passwordHash);
    }
}
