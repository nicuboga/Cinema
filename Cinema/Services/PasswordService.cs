using System.Security.Cryptography;

namespace Cinema.Services;

public static class PasswordService
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] expectedHash, byte[] salt)
    {
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
