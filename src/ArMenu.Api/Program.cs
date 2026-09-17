using ArMenu.Api;
using ArMenu.Api.Endpoints;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.Observability;
using ArMenu.Application;
using ArMenu.Infrastructure;
using ArMenu.Infrastructure.Assets;
using ArMenu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration)
    .AddObservability(builder.Configuration);

var app = builder.Build();

// First: everything after it, rate limiting and HTTPS checks included, must see the client's address and scheme.
app.UseForwardedHeaders();
app.UseSecurityHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantResolution();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthCheckTags.Ready) }).AllowAnonymous();
app.MapEndpointModules();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    await app.Services.InitializeDevelopmentDatabaseAsync();
    await app.Services.InitializeDevelopmentStorageAsync();
}

await app.RunAsync();
