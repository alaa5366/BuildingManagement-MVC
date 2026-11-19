// =====================================================================
//  PasswordHasher — BCrypt wrapper
//  بيستخدم BCrypt.Net-Next للـ Hashing
// =====================================================================
using BCrypt.Net;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Hashes and verifies passwords and PINs.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the operation on the i password hasher.
    /// </summary>
    string Hash(string password);
    /// <summary>
    /// Verifies the operation on the i password hasher.
    /// </summary>
    bool Verify(string password, string hash);
    /// <summary>
    /// Hashes the PIN.
    /// </summary>
    string HashPin(string pin);
    /// <summary>
    /// Verifies the PIN.
    /// </summary>
    bool VerifyPin(string pin, string hash);
}

/// <summary>
/// BCrypt implementation of <see cref="IPasswordHasher"/>.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes the operation on the BCrypt password hasher.
    /// </summary>
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies the operation on the BCrypt password hasher.
    /// </summary>
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

    /// <summary>
    /// Hashes the PIN.
    /// </summary>
    public string HashPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN cannot be empty", nameof(pin));

        return BCrypt.Net.BCrypt.HashPassword(pin, 10);
    }

    /// <summary>
    /// Verifies the PIN.
    /// </summary>
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
