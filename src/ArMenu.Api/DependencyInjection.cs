using System.Diagnostics;
using System.Text.Json.Serialization;
using ArMenu.Api.Authentication;
using ArMenu.Api.Endpoints;
using ArMenu.Api.Http;
using ArMenu.Api.OpenApi;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Behaviors;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ArMenu.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator(options =>
        {
            // Handlers depend on scoped services (DbContext, tenant context), so the mediator is scoped as well.
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Assemblies = [typeof(Application.DependencyInjection), typeof(Infrastructure.DependencyInjection)];
            options.PipelineBehaviors = [typeof(TelemetryBehavior<,>), typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
        });

        services.Configure<UserSessionOptions>(configuration.GetSection(UserSessionOptions.SectionName));

        // RFC 9457 problem details for every error response, correlated with distributed traces.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<JwtBearerOptionsSetup>();
        services.AddArMenuAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddReverseProxySupport(configuration);
        services.AddArMenuRateLimiting(configuration);
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            // Content-Disposition names downloaded files, such as the menu export.
            .WithExposedHeaders("Location", "Retry-After", "Content-Disposition")));

        // One JSON representation per type. The web defaults also read numbers from strings, which would turn every
        // number of the published contract into "number or string" for generated clients.
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

        // The OpenAPI document is the contract the web apps generate their types from (contracts/openapi/v1.json).
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
            options.AddOperationTransformer<TenantRouteParameterTransformer>();
            options.AddSchemaTransformer<ProblemDetailsSchemaTransformer>();
        });

        services.AddEndpointModules(typeof(DependencyInjection).Assembly);

        return services;
    }
}
