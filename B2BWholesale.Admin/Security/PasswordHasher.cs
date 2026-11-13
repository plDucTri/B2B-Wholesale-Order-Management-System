using System.Security.Cryptography;

namespace B2BWholesale.Admin.Security;

/// <summary>
/// PBKDF2-SHA256 with a random salt. Stored format: PBKDF2$iterations$saltBase64$hashBase64
/// The ASP.NET Agent Portal MUST use this same format so both apps share one Users table.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        try
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iter)) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iter, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false; // e.g. old placeholder values like DEMO_HASH_ADMIN
        }
    }
}
