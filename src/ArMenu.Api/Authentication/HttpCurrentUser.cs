using System.Security.Claims;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Users;

namespace ArMenu.Api.Authentication;

/// <summary>The account of the request's verified access token, if any.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ArMenuClaimTypes.UserId), out var id) ? Domain.Users.UserId.From(id) : null;
}
