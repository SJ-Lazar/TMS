using System.Security.Cryptography;
using System.Text;

namespace TMS.Infrastructure.Persistence.Encryption;

public static class DatabaseEncryption
{
    private static byte[]? _key;
    private static bool _initialized;

    public static void Configure(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            _key = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        }

        if (_key.Length < 32)
        {
            var padded = new byte[32];
            Array.Copy(_key, padded, Math.Min(_key.Length, padded.Length));
            _key = padded;
        }
        else if (_key.Length > 32)
        {
            _key = _key.Take(32).ToArray();
        }

        _initialized = true;
    }

    public static byte[] Encrypt(byte[] plain)
    {
        EnsureConfigured();
        using var aes = Aes.Create();
        aes.Key = _key!;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var output = new byte[aes.IV.Length + cipher.Length];
        Array.Copy(aes.IV, 0, output, 0, aes.IV.Length);
        Array.Copy(cipher, 0, output, aes.IV.Length, cipher.Length);
        return output;
    }

    public static byte[] Decrypt(byte[] cipherWithIv)
    {
        EnsureConfigured();
        using var aes = Aes.Create();
        aes.Key = _key!;

        var ivLength = aes.BlockSize / 8;
        var iv = cipherWithIv.Take(ivLength).ToArray();
        var cipher = cipherWithIv.Skip(ivLength).ToArray();

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private static void EnsureConfigured()
    {
        if (!_initialized || _key is null)
        {
            throw new InvalidOperationException("Database encryption is not configured. Call Configure with a valid key.");
        }
    }
}
