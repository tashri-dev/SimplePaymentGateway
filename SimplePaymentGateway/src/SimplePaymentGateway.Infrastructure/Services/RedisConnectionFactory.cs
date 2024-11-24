using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimplePaymentGateway.Infrastructure.Contracts;
using SimplePaymentGateway.Infrastructure.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePaymentGateway.Infrastructure.Services;

//public class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
//{
//    private readonly RedisOptions _options;
//    private readonly ILogger<RedisConnectionFactory> _logger;
//    private IConnectionMultiplexer _connection;
//    private readonly SemaphoreSlim _semaphore = new(1, 1);
//    private bool _disposed;

//    public RedisConnectionFactory(
//        IOptions<RedisOptions> options,
//        ILogger<RedisConnectionFactory> logger)
//    {
//        _options = options.Value;
//        _logger = logger;
//        CreateConnection();
//    }

//    public IConnectionMultiplexer Connection
//    {
//        get
//        {
//            if (_connection != null && _connection.IsConnected)
//                return _connection;

//            CreateConnection();
//            return _connection;
//        }
//    }

//    public bool IsConnected => _connection?.IsConnected ?? false;

//    public IDatabase GetDatabase(int? db = null)
//    {
//        return Connection.GetDatabase(db ?? -1);
//    }

//    public async Task ReconnectAsync()
//    {
//        await _semaphore.WaitAsync();
//        try
//        {
//            if (_connection != null)
//            {
//                await _connection.CloseAsync();
//                _connection.Dispose();
//            }

//            CreateConnection();
//            _logger.LogInformation("Redis connection recreated successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error reconnecting to Redis");
//            throw;
//        }
//        finally
//        {
//            _semaphore.Release();
//        }
//    }

//    private void CreateConnection()
//    {
//        try
//        {
//            var config = new ConfigurationOptions
//            {
//                EndPoints = { _options.ConnectionString },
//                Password = _options.Password,
//                AbortOnConnectFail = _options.AbortOnConnectFail,
//                ConnectTimeout = _options.ConnectTimeout,
//                SyncTimeout = _options.SyncTimeout,
//                AllowAdmin = true,
//                ConnectRetry = _options.RetryCount,
//                DefaultDatabase = 0,
//                ReconnectRetryPolicy = new LinearRetry(_options.RetryDelayMilliseconds)
//            };

//            _connection = ConnectionMultiplexer.Connect(config);

//            _connection.ConnectionFailed += (sender, args) =>
//            {
//                _logger.LogError("Redis connection failed: {FailureType}", args.FailureType);
//            };

//            _connection.ConnectionRestored += (sender, args) =>
//            {
//                _logger.LogInformation("Redis connection restored");
//            };

//            _connection.ErrorMessage += (sender, args) =>
//            {
//                _logger.LogError("Redis error: {Message}", args.Message);
//            };

//            _logger.LogInformation("Redis connection created successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error creating Redis connection");
//            throw;
//        }
//    }

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    protected virtual void Dispose(bool disposing)
//    {
//        if (_disposed)
//            return;

//        if (disposing)
//        {
//            _connection?.Dispose();
//            _semaphore.Dispose();
//        }

//        _disposed = true;
//    }
//}

public class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly RedisOptions _options;
    private readonly ILogger<RedisConnectionFactory> _logger;
    private IConnectionMultiplexer _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public RedisConnectionFactory(
        IOptions<RedisOptions> options,
        ILogger<RedisConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
        CreateConnection();
    }

    public IConnectionMultiplexer Connection
    {
        get
        {
            if (_connection != null && _connection.IsConnected)
                return _connection;

            CreateConnection();
            return _connection!;
        }
    }

    public bool IsConnected => _connection?.IsConnected ?? false;

    public IDatabase GetDatabase(int? db = null)
    {
        return Connection.GetDatabase(db ?? _options.Database);
    }

    public async Task ReconnectAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }

            CreateConnection();
            _logger.LogInformation("Redis connection recreated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconnecting to Redis");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CreateConnection()
    {
        try
        {
            _logger.LogInformation("Creating Redis connection to {Host}:{Port}", _options.Host, _options.Port);

            var config = _options.GetConfigurationOptions();

            _connection = ConnectionMultiplexer.Connect(config);

            _connection.ConnectionFailed += (sender, args) =>
            {
                _logger.LogError("Redis connection failed to {Host}:{Port}: {FailureType}",
                    _options.Host, _options.Port, args.FailureType);
            };

            _connection.ConnectionRestored += (sender, args) =>
            {
                _logger.LogInformation("Redis connection restored to {Host}:{Port}",
                    _options.Host, _options.Port);
            };

            _connection.ErrorMessage += (sender, args) =>
            {
                _logger.LogError("Redis error for {Host}:{Port}: {Message}",
                    _options.Host, _options.Port, args.Message);
            };

            _logger.LogInformation("Redis connection created successfully to {Host}:{Port}",
                _options.Host, _options.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Redis connection to {Host}:{Port}",
                _options.Host, _options.Port);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _connection?.Dispose();
            _semaphore.Dispose();
        }

        _disposed = true;
    }
}
