namespace SimplePaymentGateway.Domain.contracts;

public interface IKeyManager
{
    Task<string> GetKey(string keyIdentifier);
    Task StoreKey(string keyIdentifier, string key, TimeSpan? expiry = null);
    Task<bool> RemoveKey(string keyIdentifier);
    Task<bool> KeyExists(string keyIdentifier);
}
