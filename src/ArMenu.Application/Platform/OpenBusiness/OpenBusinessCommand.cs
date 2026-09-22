using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Platform.OpenBusiness;

/// <summary>
/// The platform administrator opens a business together with its owner's account, which signs in with the owner's
/// user name. Nothing is e-mailed: the administrator hands the name and password over. Who is asking comes from the
/// access token and is confirmed against the account.
/// </summary>
public sealed record OpenBusinessCommand(
    UserId AdministratorId,
    string BusinessName,
    string Slug,
    string DefaultCulture,
    string Currency,
    string OwnerFullName,
    string OwnerUserName,
    string OwnerPassword,
    string? TimeZone = null) : ICommand<Result<OpenedBusiness>>
{
    public override string ToString() => $"OpenBusinessCommand {{ Slug = {Slug}, *** }}";
}

public sealed record OpenedBusiness(TenantId TenantId, string Slug);
