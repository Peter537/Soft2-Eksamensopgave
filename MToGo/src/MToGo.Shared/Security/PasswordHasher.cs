namespace MToGo.Shared.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Matching Legacy system

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password ?? string.Empty, workFactor: WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return !string.IsNullOrEmpty(password) && 
                   BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
