using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ArMenu.Api.OpenApi;

/// <summary>
/// Documents the extension members of this API's problem responses, so clients can branch on stable codes instead of
/// parsing human-readable text.
/// </summary>
internal sealed class ProblemDetailsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        var type = context.JsonTypeInfo.Type;
        if (!typeof(ProblemDetails).IsAssignableFrom(type))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties["code"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "Stable, machine-readable error code, e.g. `tenant.not_found`.",
        };
        schema.Properties["traceId"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "Correlates the response with server logs and traces.",
        };

        if (type == typeof(HttpValidationProblemDetails))
        {
            schema.Properties["errorCodes"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalProperties = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.String },
                },
                Description = "Stable error codes per field (JSON path), for localized messages on the client.",
            };
        }

        return Task.CompletedTask;
    }
}
