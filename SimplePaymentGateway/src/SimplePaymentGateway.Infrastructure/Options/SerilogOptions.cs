namespace SimplePaymentGateway.Infrastructure.Options;

public class SerilogOptions
{
    public static readonly string ConfigSection = "Serilog";
    public string MinimumLevel { get; set; }
    public bool EnableConsole { get; set; }
    public bool EnableFile { get; set; }
    public string LogFilePath { get; set; }
    public ElasticsearchOptions Elasticsearch { get; set; }
}

public class ElasticsearchOptions
{
    public string Url { get; set; }
    public string IndexFormat { get; set; }
    public TimeSpan BufferRetentionPeriod { get; set; }
    public int BufferSize { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

