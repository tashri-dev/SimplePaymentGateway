// Program.cs
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using SimplePaymentGateway.API.Extyensions;
using SimplePaymentGateway.Infrastructure.Extensions;
using Serilog;
using SimplePaymentGateway.API.Configurations.Swagger;
using SimplePaymentGateway.Application.Common;
using Microsoft.Extensions.Options;




var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Configure Serilog and Elasticsearch
var environment = builder.Environment;

// Add controllers with JSON configuration
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure Swagger/OpenAPI
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Simple Payment Gateway API",
        Version = "v1",
        Description = "Simple Payment Gateway API with encryption",
        Contact = new OpenApiContact
        {
            Name = "Taha Ashri",
            Email = "taha.ashri.dv@gmail.com"
        }
    });

    // Add proper response documentation
    c.OperationFilter<AddRequiredHeaderParameter>();
    //c.UseInlineDefinitionsForEnums();
    //c.UseAllOfToExtendReferenceSchemas();
});
// Add CORS
services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add service registrations
services.AddInfrastructureServices(builder.Configuration, environment);

// Build the application
var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = false;
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
}

// Exception handling and logging middleware
app.UseRequestResponseLogging();
app.UseErrorHandling();

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");

// Map controllers
app.MapControllers();

try
{
    Log.Information("Starting Payment Gateway API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}