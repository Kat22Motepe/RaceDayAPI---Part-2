using System.Security.Cryptography;

namespace RaceDay.API.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static (string hash, string salt) Hash(string password)
    {
        // Make a random salt (the secret ingredient)
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Scramble the password with the salt 100,000 times
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // Convert to text so we can save it in the database
        return (Convert.ToBase64String(key), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string hashB64, string saltB64)
    {
        // Take the salt from the database and unscramble it
        var salt = Convert.FromBase64String(saltB64);

        // Scramble the password the user just typed in, using the SAME salt
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        // Compare the two hashes. 
        // FixedTimeEquals prevents hackers from guessing the password by timing how long it takes.
        return CryptographicOperations.FixedTimeEquals(key, Convert.FromBase64String(hashB64));
    }
}
