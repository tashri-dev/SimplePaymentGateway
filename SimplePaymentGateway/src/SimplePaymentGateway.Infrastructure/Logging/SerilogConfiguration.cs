using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog;
using SimplePaymentGateway.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog.Sinks.Elasticsearch;
using Serilog.Formatting.Elasticsearch;


namespace SimplePaymentGateway.Infrastructure.Logging;

public static class SerilogConfigurationExtensions
{
    public static LoggerConfiguration ConfigureSerilog(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = new SerilogOptions();
        configuration.GetSection(SerilogOptions.ConfigSection).Bind(options);

        loggerConfiguration
            .MinimumLevel.Is(Enum.Parse<LogEventLevel>(options.MinimumLevel))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "PaymentGateway")
            .Enrich.WithProperty("Environment", environment.EnvironmentName);

        // Console logging
        if (options.EnableConsole)
        {
            loggerConfiguration.WriteTo.Console(new JsonFormatter());
        }

        // File logging
        if (options.EnableFile)
        {
            loggerConfiguration.WriteTo.File(
                new JsonFormatter(),
                options.LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7);
        }

        // Elasticsearch logging
        if (options.Elasticsearch?.Url != null)
        {
            var elasticOptions = new ElasticsearchSinkOptions(new Uri(options.Elasticsearch.Url))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                IndexFormat = options.Elasticsearch.IndexFormat ??
                    $"payment-gateway-{environment.EnvironmentName.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                BufferBaseFilename = Path.Combine(
                    Path.GetTempPath(),
                    "payment-gateway-elastic-buffer"),
                BufferLogShippingInterval = options.Elasticsearch.BufferRetentionPeriod,
                BufferFileCountLimit = options.Elasticsearch.BufferSize,
                BatchPostingLimit = 50,
                DetectElasticsearchVersion = true,
                NumberOfShards = 2,
                NumberOfReplicas = 1,
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                 EmitEventFailureHandling.WriteToFailureSink |
                                 EmitEventFailureHandling.RaiseCallback,
                FailureCallback = (logEvent, ex) => Console.WriteLine("Elasticsearch sink error: " + ex.Message),
                CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage: true),
                ModifyConnectionSettings = connectionConfig =>
                {
                    if (!string.IsNullOrEmpty(options.Elasticsearch.Username) &&
                        !string.IsNullOrEmpty(options.Elasticsearch.Password))
                    {
                        connectionConfig.BasicAuthentication(
                            options.Elasticsearch.Username,
                            options.Elasticsearch.Password);
                    }
                    return connectionConfig;
                }
            };

            loggerConfiguration.WriteTo.Elasticsearch(elasticOptions);
        }

        return loggerConfiguration;
    }
}
