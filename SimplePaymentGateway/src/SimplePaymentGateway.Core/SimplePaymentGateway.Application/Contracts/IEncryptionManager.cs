namespace SimplePaymentGateway.Application.Contracts;

public interface IEncryptionManager
{
    string GenerateKey();
    string GenerateIV();
    string Encrypt(string data, string key);
    string Decrypt(string encryptedData, string key);
    bool ValidateKey(string key);
}
