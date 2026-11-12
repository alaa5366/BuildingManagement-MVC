// =====================================================================
//  PasswordHasher — BCrypt wrapper
//  بيستخدم BCrypt.Net-Next للـ Hashing
// =====================================================================
using BCrypt.Net;

namespace BuildingManagementMvc.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    string HashPin(string pin);
    bool VerifyPin(string pin, string hash);
}

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public string HashPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN cannot be empty", nameof(pin));

        return BCrypt.Net.BCrypt.HashPassword(pin, 10);
    }

    public bool VerifyPin(string pin, string hash)
    {
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }
        catch
        {
            return false;
        }
    }
}