using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplePaymentGateway.Application.Contracts;
using SimplePaymentGateway.Infrastructure.Options;
using SimplePaymentGateway.Infrastructure.Services.KeyManagement;
using SimplePaymentGateway.Infrastructure.Services;
using StackExchange.Redis;
using SimplePaymentGateway.Infrastructure.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using SimplePaymentGateway.Infrastructure.Logging;
using HealthChecks.Redis;

namespace SimplePaymentGateway.Infrastructure.Extensions;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddInfrastructureServices(
//        this IServiceCollection services,
//        IConfiguration configuration,
//        IHostEnvironment environment)
//    {
//        // Configure Redis
//        services.Configure<RedisOptions>(options =>
//            configuration.GetSection("Redis").Bind(options));

//        // Add Redis Connection Factory as Singleton
//        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

//        // Register services
//        services.AddScoped<ITransactionProcessor, TransactionProcessor>();
//        services.AddScoped<IEncryptionManager, EncryptionManager>();
//        services.AddScoped<IKeyManager, RedisKeyManager>();

//        // Add health checks
//        var redisOptions = configuration.GetSection("Redis").Get<RedisOptions>();
//        var redisConfig = new ConfigurationOptions
//        {
//            EndPoints = { redisOptions.ConnectionString },
//            Password = redisOptions.Password,
//            AbortOnConnectFail = redisOptions.AbortOnConnectFail,
//            ConnectTimeout = redisOptions.ConnectTimeout,
//            SyncTimeout = redisOptions.SyncTimeout
//        };

//        services.AddHealthChecks()
//            .AddRedis(
//                redisConfig.ToString(),
//                name: "redis",
//                tags: new[] { "services" })
//            .AddElasticsearch(
//                configuration["Serilog:Elasticsearch:Url"],
//                name: "elasticsearch",
//                tags: new[] { "services" });

//        return services;
//    }
//}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configure Serilog
        ConfigureLogging(configuration, environment);

        // Configure Redis
        services.Configure<RedisOptions>(options =>
            configuration.GetSection("Redis").Bind(options));
        services.Configure<SerilogOptions>(options =>
            configuration.GetSection(SerilogOptions.ConfigSection).Bind(options));

        // Add Redis Connection Factory as Singleton
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

        // Register services
        services.AddScoped<ITransactionProcessor, TransactionProcessor>();
        services.AddScoped<IEncryptionManager, EncryptionManager>();
        services.AddScoped<IKeyManager, RedisKeyManager>();

        // Add health checks
        ConfigureHealthChecks(services, configuration);

        return services;
    }

    private static void ConfigureLogging(IConfiguration configuration, IHostEnvironment environment)
    {
        Log.Logger = new LoggerConfiguration()
            .ConfigureSerilog(configuration, environment)
            .CreateLogger();
    }

    private static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection("Redis").Get<RedisOptions>();
        var redisConfig = redisOptions?.GetConfigurationOptions();

        services.AddHealthChecks()
            .AddRedis(
                redisConfig.ToString(),
                name: "redis",
                tags: new[] { "services" })
            .AddElasticsearch(
                configuration["Serilog:Elasticsearch:Url"],
                name: "elasticsearch",
                tags: new[] { "services" });
    }
}
