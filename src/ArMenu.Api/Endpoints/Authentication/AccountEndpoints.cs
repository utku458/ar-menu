using System.Security.Claims;
using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Accounts.DeleteAccount;
using ArMenu.Application.Accounts.Queries.GetAccountDeletion;
using ArMenu.Application.Authentication.Queries.GetMyWorkspaces;
using ArMenu.Application.Authentication.RequestPasswordReset;
using ArMenu.Application.Authentication.ResetPassword;
using ArMenu.Application.Authentication.SendEmailVerification;
using ArMenu.Application.Authentication.VerifyEmail;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Api.Endpoints.Authentication;

/// <summary>
/// A person's own account, across every business they belong to: recovering the password, verifying the address and
/// deleting the account.
/// Accounts are global, so these endpoints have no workspace; the links they rely on carry their secrets in POST bodies.
/// </summary>
internal sealed class AccountEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/api/v1/auth")
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Account");

        account.MapPost("/password-reset", RequestPasswordResetAsync)
            .WithSummary("E-mails a password reset link if the address has an account. Always 202, whether or not it has one.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        account.MapPost("/password-reset/confirm", ResetPasswordAsync)
            .WithSummary("Sets a new password with a reset link. Existing sessions of the account can no longer be refreshed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        account.MapPost("/email-verification/confirm", VerifyEmailAsync)
            .WithSummary("Confirms the account's e-mail address with the link sent to it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/api/v1/me/workspaces", GetWorkspacesAsync)
            .RequireAuthorization()
            .WithTags("Account")
            .WithSummary("The businesses the signed-in person belongs to, with their role in each. Each needs its own sign-in.")
            .Produces<IReadOnlyList<WorkspaceResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapPost("/api/v1/me/email-verification", SendEmailVerificationAsync)
            .RequireAuthorization()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Account")
            .WithSummary("E-mails the signed-in user a new verification link, unless the address is already verified.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/v1/me/deletion", GetDeletionAsync)
            .RequireAuthorization()
            .WithTags("Account")
            .WithSummary("What deleting the account would do: businesses that close with it, must be handed over first, or are left.")
            .Produces<AccountDeletionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapPost("/api/v1/me/deletion", DeleteAccountAsync)
            .RequireAuthorization()
            // The business of the access token names the refresh cookie to remove.
            .RequireTenantFromClaims()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Account")
            .WithSummary("Deletes the account for good, confirmed with the password. Refused while the person owns a business with other members.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RequestPasswordResetAsync(PasswordResetRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RequestPasswordResetCommand(request.Email, request.Language), cancellationToken);
        return result.IsSuccess ? TypedResults.Accepted((string?)null) : result.Error.ToProblem();
    }

    private static async Task<IResult> ResetPasswordAsync(NewPasswordRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        return (await mediator.Send(new ResetPasswordCommand(request.Token, request.Password), cancellationToken)).ToNoContent();
    }

    private static async Task<IResult> VerifyEmailAsync(EmailVerificationRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new VerifyEmailCommand(request.Token), cancellationToken)).ToNoContent();

    private static async Task<IResult> GetWorkspacesAsync(ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyWorkspacesQuery(UserIdOf(user)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> SendEmailVerificationAsync(ResendVerificationRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SendEmailVerificationCommand(UserIdOf(user), request.Language), cancellationToken);
        return result.IsSuccess ? TypedResults.Accepted((string?)null) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetDeletionAsync(ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccountDeletionQuery(UserIdOf(user)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAccountAsync(DeleteAccountRequest request, ClaimsPrincipal user, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAccountCommand(UserIdOf(user), request.Password, request.Language), cancellationToken);
        if (result.IsSuccess)
        {
            // The sessions are gone with the memberships; the browser's cookie for this business goes too.
            RefreshTokenCookie.Delete(httpContext);
        }

        return result.ToNoContent();
    }

    private static UserId UserIdOf(ClaimsPrincipal user) => UserId.From(Guid.Parse(user.FindFirstValue(ArMenuClaimTypes.UserId)!));

    internal sealed record PasswordResetRequest(string Email, string? Language)
    {
        public override string ToString() => "PasswordResetRequest { *** }";
    }

    internal sealed record NewPasswordRequest(string Token, string Password)
    {
        public override string ToString() => "NewPasswordRequest { *** }";
    }

    internal sealed record EmailVerificationRequest(string Token)
    {
        public override string ToString() => "EmailVerificationRequest { *** }";
    }

    internal sealed record ResendVerificationRequest(string? Language);

    internal sealed record DeleteAccountRequest(string Password, string? Language)
    {
        public override string ToString() => "DeleteAccountRequest { *** }";
    }
}
