using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Infrastructure.Contracts;
using SimplePaymentGateway.Infrastructure.Exceptions;
using SimplePaymentGateway.Infrastructure.Options;
using StackExchange.Redis;
using System.IO.Compression;

namespace SimplePaymentGateway.Infrastructure.Services.KeyManagement;

public class RedisKeyManager : IKeyManager
{
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisKeyManager> _logger;

    public RedisKeyManager(
        IRedisConnectionFactory redisFactory,
        IOptions<RedisOptions> options,
        ILogger<RedisKeyManager> logger)
    {
        _redisFactory = redisFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetKey(string keyIdentifier)
    {
        try
        {
            var db = _redisFactory.GetDatabase();
            var key = GetFullKeyName(keyIdentifier);
            var value = await ExecuteWithRetryAsync(async () =>
                await db.StringGetAsync(key));

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
            var db = _redisFactory.GetDatabase();
            var fullKey = GetFullKeyName(keyIdentifier);
            var value = _options.EnableCompression
                ? await CompressValue(key)
                : key;

            await ExecuteWithRetryAsync(async () =>
                await db.StringSetAsync(
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
            var db = _redisFactory.GetDatabase();
            var key = GetFullKeyName(keyIdentifier);
            var result = await ExecuteWithRetryAsync(async () =>
                await db.KeyDeleteAsync(key));

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
            var db = _redisFactory.GetDatabase();
            var key = GetFullKeyName(keyIdentifier);
            return await ExecuteWithRetryAsync(async () =>
                await db.KeyExistsAsync(key));
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
                await Task.Delay(_options.RetryDelayMilliseconds * retryCount);
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
