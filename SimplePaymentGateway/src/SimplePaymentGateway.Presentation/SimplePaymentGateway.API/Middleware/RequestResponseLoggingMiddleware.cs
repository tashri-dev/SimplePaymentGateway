using System.Diagnostics;

namespace SimplePaymentGateway.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly HashSet<string> _allowedContentTypes = new()
    {
        "application/json",
        "text/plain",
        "text/json"
    };

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        _logger.LogInformation(
            "Request {RequestId}: {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        var originalBodyStream = context.Response.Body;

        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);

            sw.Stop();

            if (ShouldLogResponse(context))
            {
                memoryStream.Position = 0;
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                if (responseBody.Length <= 4096) // Don't log large responses
                {
                    _logger.LogInformation(
                        "Request {RequestId}: Completed in {ElapsedMs}ms with status code {StatusCode}. Response: {Response}",
                        requestId,
                        sw.ElapsedMilliseconds,
                        context.Response.StatusCode,
                        responseBody);
                }
                else
                {
                    _logger.LogInformation(
                        "Request {RequestId}: Completed in {ElapsedMs}ms with status code {StatusCode}. Response too large to log ({Size} bytes)",
                        requestId,
                        sw.ElapsedMilliseconds,
                        context.Response.StatusCode,
                        responseBody.Length);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Request {RequestId}: Completed in {ElapsedMs}ms with status code {StatusCode}. Response type: {ContentType}",
                    requestId,
                    sw.ElapsedMilliseconds,
                    context.Response.StatusCode,
                    context.Response.ContentType);
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldLogResponse(HttpContext context)
    {
        return !string.IsNullOrEmpty(context.Response.ContentType) &&
               _allowedContentTypes.Any(t => context.Response.ContentType.StartsWith(t));
    }
}