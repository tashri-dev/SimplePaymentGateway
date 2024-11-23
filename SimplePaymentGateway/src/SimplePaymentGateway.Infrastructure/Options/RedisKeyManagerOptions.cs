namespace SimplePaymentGateway.Infrastructure.Options;

public class RedisKeyManagerOptions
{
    public const string ConfigSection = "Redis";

    public string ConnectionString { get; set; }
    public string InstanceName { get; set; } = "PaymentGateway_";
    public string KeyPrefix { get; set; } = "keys:";
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableCompression { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(300);
}
