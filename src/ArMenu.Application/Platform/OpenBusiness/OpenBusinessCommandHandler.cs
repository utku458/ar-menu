using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Platform.OpenBusiness;

/// <remarks>
/// Sign-up's steps, without the parts that assume the owner is the one typing: no verification e-mail (the account
/// has no mailbox) and no session (the administrator is the one signed in, and stays in the platform workspace).
/// </remarks>
public sealed class OpenBusinessCommandHandler(
    PlatformAdministrator administrator,
    ITenantRepository tenants,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    IPasswordHasher passwordHasher,
    ITenantContextSetter tenantContextSetter,
    ITenantLookup tenantLookup,
    IUnitOfWork unitOfWork)
    : ICommandHandler<OpenBusinessCommand, Result<OpenedBusiness>>
{
    public async ValueTask<Result<OpenedBusiness>> Handle(OpenBusinessCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var caller = await administrator.RequireAsync(command.AdministratorId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var slug = TenantSlug.Create(command.Slug);
        var defaultCulture = CultureCode.Create(command.DefaultCulture);
        var currency = Currency.Create(command.Currency);
        var userName = UserName.Create(command.OwnerUserName);

        if (FirstError(slug, defaultCulture, currency, userName) is { } invalidInput)
        {
            return invalidInput;
        }

        if (await tenants.SlugExistsAsync(slug.Value, cancellationToken))
        {
            return TenantErrors.SlugTaken;
        }

        if (await users.UserNameExistsAsync(userName.Value, cancellationToken))
        {
            return UserErrors.UserNameTaken;
        }

        var timeZone = TenantTimeZone.Create(command.TimeZone) is { IsSuccess: true } zone ? zone.Value : TenantTimeZone.Utc;
        var tenant = Tenant.Create(command.BusinessName, slug.Value, defaultCulture.Value, currency.Value, timeZone);
        if (tenant.IsFailure)
        {
            return tenant.Error;
        }

        var owner = User.OpenWithUserName(userName.Value, command.OwnerFullName, passwordHasher.Hash(command.OwnerPassword));
        if (owner.IsFailure)
        {
            return owner.Error;
        }

        // Everything created from here on belongs to the new business, so the scope is bound to it: the write guard
        // and row-level security then apply to the owner's membership exactly as for any other request.
        var tenantInfo = TenantInfo.From(tenant.Value);
        tenantContextSetter.SetTenant(tenantInfo);

        tenants.Add(tenant.Value);
        users.Add(owner.Value);
        memberships.Add(TenantMembership.Create(tenant.Value.Id, owner.Value.Id, TenantRole.Owner));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Someone may have opened this slug's menu URL before the business existed and cached "not found".
        await tenantLookup.InvalidateAsync(tenantInfo.Id, slug.Value, cancellationToken);

        return new OpenedBusiness(tenantInfo.Id, tenantInfo.Slug);
    }

    private static Error? FirstError(params Result[] results) =>
        results.FirstOrDefault(result => result.IsFailure)?.Error;
}
