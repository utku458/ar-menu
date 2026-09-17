using ArMenu.Application.ArModels;
using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.Emails;
using ArMenu.Application.Team;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.Application;

public static class DependencyInjection
{
    /// <remarks>
    /// Handlers and pipeline behaviors are registered by the Mediator source generator in the composition root.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);

        services.AddScoped<SessionIssuer>();
        services.AddSingleton<ArMenuMetrics>();
        services.AddScoped<InvitationMailer>();
        services.AddScoped<UserLinks>();
        services.AddScoped<Accounts.PasswordConfirmation>();
        services.AddSingleton<DashboardLinks>();

        services.AddOptions<UserSessionOptions>()
            .Validate(
                options => options.IdleTimeout > TimeSpan.Zero && options.AbsoluteLifetime >= options.IdleTimeout,
                "Session idle timeout must be positive and not longer than the absolute lifetime.")
            .ValidateOnStart();

        services.AddOptions<ArModelProcessingOptions>()
            .Validate(
                options => options.TransferUrlLifetime > TimeSpan.Zero && options.RetryDelay >= TimeSpan.Zero,
                "Model processing URL lifetime must be positive and the retry delay cannot be negative.")
            .ValidateOnStart();

        services.AddOptions<DashboardOptions>()
            .Validate(
                options => options.Url is { IsAbsoluteUri: true } url && url.AbsolutePath.EndsWith('/'),
                $"{DashboardOptions.SectionName}:Url must be an absolute URL ending with '/'.")
            .ValidateOnStart();

        return services;
    }
}
