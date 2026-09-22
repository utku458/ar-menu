using ArMenu.Domain.Common;

namespace ArMenu.Application.Platform;

public static class PlatformErrors
{
    public static readonly Error NotAdministrator = Error.Unauthorized(
        "platform.not_administrator", "Only the platform administrator can do this.");

    public static readonly Error BusinessNotFound = Error.NotFound(
        "platform.business_not_found", "No active business has this id.");
}
