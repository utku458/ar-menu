using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Authentication;
using ArMenu.Application.Emails;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Tenants.SignUp;

public sealed class SignUpCommandHandler(
    ITenantRepository tenants,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    IPasswordHasher passwordHasher,
    ITenantContextSetter tenantContextSetter,
    ITenantLookup tenantLookup,
    SessionIssuer sessionIssuer,
    UserLinks userLinks,
    IEmailOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SignUpCommand, Result<SignUpResult>>
{
    public async ValueTask<Result<SignUpResult>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slug = TenantSlug.Create(command.Slug);
        var defaultCulture = CultureCode.Create(command.DefaultCulture);
        var currency = Currency.Create(command.Currency);
        var email = Email.Create(command.OwnerEmail);

        if (FirstError(slug, defaultCulture, currency, email) is { } invalidInput)
        {
            return invalidInput;
        }

        if (await tenants.SlugExistsAsync(slug.Value, cancellationToken))
        {
            return TenantErrors.SlugTaken;
        }

        if (await users.EmailExistsAsync(email.Value, cancellationToken))
        {
            return UserErrors.EmailTaken;
        }

        var timeZone = TenantTimeZone.Create(command.TimeZone) is { IsSuccess: true } browserZone ? browserZone.Value : TenantTimeZone.Utc;
        var tenant = Tenant.Create(command.BusinessName, slug.Value, defaultCulture.Value, currency.Value, timeZone);
        if (tenant.IsFailure)
        {
            return tenant.Error;
        }

        var owner = User.Register(email.Value, command.OwnerFullName, passwordHasher.Hash(command.Password));
        if (owner.IsFailure)
        {
            return owner.Error;
        }

        // Everything created from here on belongs to the new tenant, so the scope is bound to it:
        // the write guard and row-level security then apply exactly as for any other request.
        var tenantInfo = TenantInfo.From(tenant.Value);
        tenantContextSetter.SetTenant(tenantInfo);

        var membership = TenantMembership.Create(tenant.Value.Id, owner.Value.Id, TenantRole.Owner);

        tenants.Add(tenant.Value);
        users.Add(owner.Value);
        memberships.Add(membership);
        var authentication = sessionIssuer.StartSession(owner.Value, membership);
        var verificationLink = await userLinks.IssueAsync(owner.Value, UserTokenPurpose.EmailVerification, cancellationToken);

        outbox.Add(
            AccountEmails.EmailVerification(email.Value.Value, owner.Value.FullName, verificationLink, EmailLanguage.Resolve(null, tenantInfo.DefaultCulture)),
            AccountEmails.EmailVerificationTemplate);
        await unitOfWork.SaveChangesAsync(cancellationToken);


        // Someone may have opened this slug's menu URL before sign-up and cached "not found".
        await tenantLookup.InvalidateAsync(tenantInfo.Id, slug.Value, cancellationToken);

        return new SignUpResult(tenantInfo.Id, tenantInfo.Slug, authentication);
    }

    private static Error? FirstError(params Result[] results) =>
        results.FirstOrDefault(result => result.IsFailure)?.Error;
}
