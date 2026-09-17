using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Application.Tenants.UpdateTenantBranding;
using ArMenu.Application.Tenants.UpdateTenantLanguages;
using ArMenu.Application.Tenants.UpdateTenantTimeZone;
using Mediator;

namespace ArMenu.Api.Endpoints.Settings;

internal sealed class SettingsEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.MapGroup("/api/v1/manage/settings")
            .RequireAuthorization(AuthorizationPolicies.MenuEditor)
            .RequireTenantFromClaims()
            .WithTags("Settings")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        settings.MapPut("/languages", UpdateLanguagesAsync)
            .WithSummary("Sets the languages the menu is offered in and the default one guests fall back to.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        settings.MapPut("/branding", UpdateBrandingAsync)
            .WithSummary("Sets the business's name, the logo above its menu and the colour the menu is painted with.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        settings.MapPut("/time-zone", UpdateTimeZoneAsync)
            .WithSummary("Sets the business's IANA time zone, where its days begin. Days already counted keep their boundaries.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> UpdateLanguagesAsync(LanguagesRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new UpdateTenantLanguagesCommand(request.DefaultCulture, request.SupportedCultures), cancellationToken))
            .ToNoContent();

    private static async Task<IResult> UpdateBrandingAsync(BrandingRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new UpdateTenantBrandingCommand(request.Name, request.LogoPath, request.AccentColor), cancellationToken))
            .ToNoContent();

    private static async Task<IResult> UpdateTimeZoneAsync(TimeZoneRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new UpdateTenantTimeZoneCommand(request.TimeZone), cancellationToken)).ToNoContent();

    internal sealed record LanguagesRequest(string DefaultCulture, IReadOnlyList<string> SupportedCultures);

    /// <summary>
    /// <c>LogoPath</c> is the <c>path</c> of a published logo upload, or <c>null</c> to show no logo; <c>AccentColor</c>
    /// is written <c>#rrggbb</c>, or <c>null</c> for the platform's own neutral style.
    /// </summary>
    internal sealed record BrandingRequest(string Name, string? LogoPath, string? AccentColor);

    internal sealed record TimeZoneRequest(string TimeZone);
}
