using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplePaymentGateway.Domain.contracts;
using SimplePaymentGateway.Infrastructure.Exceptions;
using SimplePaymentGateway.Infrastructure.Options;
using StackExchange.Redis;
using System.IO.Compression;

namespace SimplePaymentGateway.Infrastructure.Services.KeyManagement;

public class RedisKeyManager : IKeyManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisKeyManagerOptions _options;
    private readonly ILogger<RedisKeyManager> _logger;
    private readonly IDatabase _db;

    public RedisKeyManager(
        IConnectionMultiplexer redis,
        IOptions<RedisKeyManagerOptions> options,
        ILogger<RedisKeyManager> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
        _db = _redis.GetDatabase();
    }

    public async Task<string> GetKey(string keyIdentifier)
    {
        try
        {
            var key = GetFullKeyName(keyIdentifier);
            var value = await ExecuteWithRetryAsync(async () =>
                await _db.StringGetAsync(key));

            if (value.IsNull)
            {
                _logger.LogWarning("Key not found: {KeyIdentifier}", keyIdentifier);
                return null;
            }

            var decompressedValue = _options.EnableCompression
                ? await DecompressValue(value)
                : value.ToString();

            _logger.LogDebug("Retrieved key: {KeyIdentifier}", keyIdentifier);
            return decompressedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key: {KeyIdentifier}", keyIdentifier);
            throw new KeyManagerException("Failed to retrieve key", ex);
        }
    }

    public async Task StoreKey(string keyIdentifier, string key, TimeSpan? expiry = null)
    {
        try
        {
            var fullKey = GetFullKeyName(keyIdentifier);
            var value = _options.EnableCompression
                ? await CompressValue(key)
                : key;

            await ExecuteWithRetryAsync(async () =>
                await _db.StringSetAsync(
                    fullKey,
                    value,
                    expiry ?? _options.DefaultExpiry));

            _logger.LogInformation(
                "Stored key: {KeyIdentifier}, Expiry: {Expiry}",
                keyIdentifier,
                expiry ?? _options.DefaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing key: {KeyIdentifier}", keyIdentifier);
            throw new KeyManagerException("Failed to store key", ex);
        }
    }

    public async Task<bool> RemoveKey(string keyIdentifier)
    {
        try
        {
            var key = GetFullKeyName(keyIdentifier);
            var result = await ExecuteWithRetryAsync(async () =>
                await _db.KeyDeleteAsync(key));

            _logger.LogInformation(
                "Removed key: {KeyIdentifier}, Success: {Success}",
                keyIdentifier,
                result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key: {KeyIdentifier}", keyIdentifier);
            throw new KeyManagerException("Failed to remove key", ex);
        }
    }

    public async Task<bool> KeyExists(string keyIdentifier)
    {
        try
        {
            var key = GetFullKeyName(keyIdentifier);
            return await ExecuteWithRetryAsync(async () =>
                await _db.KeyExistsAsync(key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence: {KeyIdentifier}", keyIdentifier);
            throw new KeyManagerException("Failed to check key existence", ex);
        }
    }

    private string GetFullKeyName(string keyIdentifier) =>
        $"{_options.InstanceName}{_options.KeyPrefix}{keyIdentifier}";

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (retryCount < _options.RetryCount)
            {
                retryCount++;
                _logger.LogWarning(
                    ex,
                    "Retry {RetryCount} of {MaxRetries}",
                    retryCount,
                    _options.RetryCount);
                await Task.Delay(_options.RetryDelay * retryCount);
            }
        }
    }

    private async Task<string> CompressValue(string value)
    {
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        using (var writer = new StreamWriter(gzipStream))
        {
            await writer.WriteAsync(value);
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    private async Task<string> DecompressValue(string compressedValue)
    {
        var bytes = Convert.FromBase64String(compressedValue);
        using var memoryStream = new MemoryStream(bytes);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        return await reader.ReadToEndAsync();
    }
}
