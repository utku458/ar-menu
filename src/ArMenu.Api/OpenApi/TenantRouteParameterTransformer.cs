using ArMenu.Api.MultiTenancy;
using ArMenu.Domain.Tenants;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ArMenu.Api.OpenApi;

/// <summary>
/// Documents the tenant slug route parameter. Tenant-bound endpoints resolve the slug in middleware instead of binding
/// it to a handler argument, so API Explorer cannot see it, and clients generated from the document would miss it.
/// </summary>
internal sealed class TenantRouteParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var strategy = context.Description.ActionDescriptor.EndpointMetadata.OfType<RouteTenantResolutionStrategy>().FirstOrDefault();
        if (strategy is null)
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];
        if (operation.Parameters.Any(parameter => parameter.In == ParameterLocation.Path && parameter.Name == strategy.RouteParameterName))
        {
            return Task.CompletedTask;
        }

        operation.Parameters.Insert(0, new OpenApiParameter
        {
            Name = strategy.RouteParameterName,
            In = ParameterLocation.Path,
            Required = true,
            Description = "Slug of the business, as printed in its QR code links.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = TenantSlug.MinLength,
                MaxLength = TenantSlug.MaxLength,
            },
        });

        return Task.CompletedTask;
    }
}
