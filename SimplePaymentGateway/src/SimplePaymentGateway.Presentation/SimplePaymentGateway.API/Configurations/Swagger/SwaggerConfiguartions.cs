using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimplePaymentGateway.API.Configurations.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition("EncryptionKey", new OpenApiSecurityScheme
        {
            Description = "Encryption key for request/response encryption",
            Name = "X-Encryption-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme"
        });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "EncryptionKey"
            },
            In = ParameterLocation.Header
        };

        var requirement = new OpenApiSecurityRequirement
    {
        { scheme, new List<string>() }
    };

        options.AddSecurityRequirement(requirement);
    }
}
