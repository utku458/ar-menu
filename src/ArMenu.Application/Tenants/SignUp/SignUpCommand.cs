using ArMenu.Application.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using Mediator;

namespace ArMenu.Application.Tenants.SignUp;

/// <summary>
/// Self-service onboarding: creates a business (tenant), its owner account, and signs the owner in. The time zone is a
/// hint, the zone the owner's browser reports: an unknown name falls back to UTC rather than blocking sign-up, and the
/// owner can change it in settings.
/// </summary>
public sealed record SignUpCommand(
    string BusinessName,
    string Slug,
    string DefaultCulture,
    string Currency,
    string OwnerFullName,
    string OwnerEmail,
    string Password,
    string? TimeZone = null) : ICommand<Result<SignUpResult>>
{
    public override string ToString() => $"SignUpCommand {{ Slug = {Slug}, *** }}";
}

public sealed record SignUpResult(TenantId TenantId, string Slug, AuthenticationResult Authentication);
