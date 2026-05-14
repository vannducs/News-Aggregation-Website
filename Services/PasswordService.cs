using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace NewsAggregator.Services;

public class PasswordService
{
    private const string Prefix = "PBKDF2";
    private const int IterationCount = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: IterationCount,
            numBytesRequested: HashSize);

        return $"{Prefix}${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerificationResult VerifyPassword(string storedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return new PasswordVerificationResult(false, false);
        }

        if (!storedPassword.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            return new PasswordVerificationResult(
                IsVerified: storedPassword == providedPassword,
                NeedsRehash: storedPassword == providedPassword);
        }

        var parts = storedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        {
            return new PasswordVerificationResult(false, false);
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);
        var actualHash = KeyDerivation.Pbkdf2(
            password: providedPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: expectedHash.Length);

        var verified = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        var needsRehash = verified && iterations < IterationCount;

        return new PasswordVerificationResult(verified, needsRehash);
    }
}

public readonly record struct PasswordVerificationResult(bool IsVerified, bool NeedsRehash);
