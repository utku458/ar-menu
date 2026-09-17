namespace ArMenu.Domain.Common;

public enum ErrorType
{
    /// <summary>The input violates a business rule or invariant.</summary>
    Validation = 0,

    /// <summary>A referenced resource does not exist (in the caller's tenant).</summary>
    NotFound = 1,

    /// <summary>The operation conflicts with the current state (e.g. a slug that is already taken).</summary>
    Conflict = 2,

    /// <summary>The caller could not be authenticated (e.g. invalid credentials or an unusable refresh token).</summary>
    Unauthorized = 3,
}
