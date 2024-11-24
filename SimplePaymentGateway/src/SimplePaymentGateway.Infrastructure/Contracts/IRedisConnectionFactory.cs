using StackExchange.Redis;

namespace SimplePaymentGateway.Infrastructure.Contracts;

public interface IRedisConnectionFactory
{
    IConnectionMultiplexer Connection { get; }
    IDatabase GetDatabase(int? db = null);
    bool IsConnected { get; }
    Task ReconnectAsync();
}
