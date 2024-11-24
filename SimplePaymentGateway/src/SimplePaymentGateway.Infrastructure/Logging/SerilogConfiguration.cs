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

        // Configure minimum levels
        loggerConfiguration
            .MinimumLevel.Is(Enum.Parse<LogEventLevel>(options.MinimumLevel.Default));

        // Configure overrides
        if (options.MinimumLevel.Override != null)
        {
            foreach (var @override in options.MinimumLevel.Override)
            {
                loggerConfiguration.MinimumLevel.Override(
                    @override.Key,
                    Enum.Parse<LogEventLevel>(@override.Value));
            }
        }

        // Configure enrichers
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();

        if (options.Enrich != null)
        {
            foreach (var enricher in options.Enrich)
            {
                switch (enricher)
                {
                    case "WithThreadId":
                        loggerConfiguration.Enrich.WithThreadId();
                        break;
                        // Add other custom enrichers as needed
                }
            }
        }

        // Configure properties
        if (options.Properties != null)
        {
            foreach (var property in options.Properties)
            {
                loggerConfiguration.Enrich.WithProperty(property.Key, property.Value);
            }
        }

        // Configure sinks
        foreach (var sink in options.WriteTo)
        {
            switch (sink.Name.ToLower())
            {
                case "console":
                    loggerConfiguration.WriteTo.Console(
                        outputTemplate: sink.Args.OutputTemplate);
                    break;
                case "file":
                    loggerConfiguration.WriteTo.File(
                        path: sink.Args.Path,
                        outputTemplate: sink.Args.OutputTemplate,
                        rollingInterval: Enum.Parse<RollingInterval>(sink.Args.RollingInterval));
                    break;
            }
        }

        // Configure Elasticsearch
        if (options.Elasticsearch?.Url != null)
        {
            var elasticOptions = new ElasticsearchSinkOptions(new Uri(options.Elasticsearch.Url))
            {
                AutoRegisterTemplate = options.Elasticsearch.AutoRegisterTemplate,
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
                FailureCallback = (logEvent, ex) =>
                    Console.WriteLine("Elasticsearch sink error: " + ex.Message),
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