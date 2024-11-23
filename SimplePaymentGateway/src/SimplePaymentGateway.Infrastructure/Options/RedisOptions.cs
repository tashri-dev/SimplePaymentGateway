using StackExchange.Redis;

namespace SimplePaymentGateway.Infrastructure.Options;

public class RedisOptions
{
    public const string ConfigSection = "Redis";

    public string Host { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public int Database { get; set; }
    public string InstanceName { get; set; }
    public string KeyPrefix { get; set; }
    public TimeSpan DefaultExpiry { get; set; }
    public bool EnableCompression { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public bool AbortOnConnectFail { get; set; }
    public int ConnectTimeout { get; set; }
    public int SyncTimeout { get; set; }

    public ConfigurationOptions GetConfigurationOptions()
    {
        return new ConfigurationOptions
        {
            EndPoints = { $"{Host}:{Port}" },
            Password = Password,
            DefaultDatabase = Database,
            AbortOnConnectFail = AbortOnConnectFail,
            ConnectTimeout = ConnectTimeout,
            SyncTimeout = SyncTimeout,
            AllowAdmin = true,
            ConnectRetry = RetryCount,
            ReconnectRetryPolicy = new LinearRetry(RetryDelayMilliseconds)
        };
    }
}