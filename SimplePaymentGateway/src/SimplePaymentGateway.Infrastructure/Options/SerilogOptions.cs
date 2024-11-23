namespace SimplePaymentGateway.Infrastructure.Options;

public class SerilogOptions
{
    public const string ConfigSection = "Serilog";

    public MinimumLevelOptions MinimumLevel { get; set; }
    public List<WriteTo> WriteTo { get; set; }
    public ElasticsearchOptions Elasticsearch { get; set; }
    public List<string> Enrich { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}

public class MinimumLevelOptions
{
    public string Default { get; set; }
    public Dictionary<string, string> Override { get; set; }
}

public class WriteTo
{
    public string Name { get; set; }
    public WriteToArgs Args { get; set; }
}

public class WriteToArgs
{
    public string OutputTemplate { get; set; }
    public string Path { get; set; }
    public string RollingInterval { get; set; }
}

public class ElasticsearchOptions
{
    public string Url { get; set; }
    public string IndexFormat { get; set; }
    public bool AutoRegisterTemplate { get; set; }
    public string AutoRegisterTemplateVersion { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int BufferSize { get; set; }
    public TimeSpan BufferRetentionPeriod { get; set; }
}
