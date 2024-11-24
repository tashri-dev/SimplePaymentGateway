using Microsoft.Extensions.Logging;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Infrastructure.Exceptions;
using System.Security.Cryptography;

namespace SimplePaymentGateway.Infrastructure.Services;

public class EncryptionManager : IEncryptionManager
{
    private const int KeySize = 32;  // 256 bits
    private const int IvSize = 16;   // 128 bits
    private readonly ILogger<EncryptionManager> _logger;

    public EncryptionManager(ILogger<EncryptionManager> logger)
    {
        _logger = logger;
    }

    public string GenerateKey()
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var key = new byte[KeySize];
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key");
            throw new EncryptionException("Failed to generate encryption key", ex);
        }
    }

    public string GenerateIV()
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var iv = new byte[IvSize];
            rng.GetBytes(iv);
            return Convert.ToBase64String(iv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating IV");
            throw new EncryptionException("Failed to generate IV", ex);
        }
    }

    public string Encrypt(string data, string key)
    {
        try
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data to encrypt cannot be null or empty");

            if (!ValidateKey(key))
                throw new ArgumentException("Invalid encryption key");

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            var iv = Convert.FromBase64String(GenerateIV());
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();

            // Write IV first
            msEncrypt.Write(iv, 0, iv.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(data);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption error");
            throw new EncryptionException("Failed to encrypt data", ex);
        }
    }

    public string Decrypt(string encryptedData, string key)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedData))
                throw new ArgumentException("Encrypted data cannot be null or empty");

            if (!ValidateKey(key))
                throw new ArgumentException("Invalid encryption key");

            var fullCipher = Convert.FromBase64String(encryptedData);

            if (fullCipher.Length < IvSize)
                throw new ArgumentException("Invalid encrypted data format");

            var iv = new byte[IvSize];
            var cipherText = new byte[fullCipher.Length - IvSize];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, IvSize);
            Buffer.BlockCopy(fullCipher, IvSize, cipherText, 0, cipherText.Length);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption error");
            throw new EncryptionException("Failed to decrypt data", ex);
        }
    }

    public bool ValidateKey(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var keyBytes = Convert.FromBase64String(key);
            return keyBytes.Length == KeySize;
        }
        catch
        {
            return false;
        }
    }
}

// Domain/Exceptions/EncryptionException.cs
public class EncryptionException : Exception
{
    public EncryptionException(string message) : base(message)
    {
    }

    public EncryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}