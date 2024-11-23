using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimplePaymentGateway.API.Configurations.Swagger;

public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // List of endpoints that require the encryption key
        var requiredEndpoints = new List<string>
        {
            "api/Transaction/process"
            // Add more endpoints as needed
        };

        // Check if the current endpoint is in the list of required endpoints
        if (requiredEndpoints.Contains(context.ApiDescription.RelativePath))
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Encryption-Key",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                },
                Description = "Encryption key obtained from /api/encryption/key endpoint"
            });
        }
    }
}

