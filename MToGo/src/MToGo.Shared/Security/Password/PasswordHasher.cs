namespace MToGo.Shared.Security.Password;

using BCrypt.Net;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.HashPassword(password ?? string.Empty, workFactor: WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return !string.IsNullOrEmpty(password) && BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
