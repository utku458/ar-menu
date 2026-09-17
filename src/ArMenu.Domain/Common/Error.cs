namespace ArMenu.Domain.Common;

/// <summary>
/// An expected, recoverable failure of a domain or application operation
/// (as opposed to exceptions, which signal bugs or infrastructure faults).
/// </summary>
/// <param name="Code">Stable, machine-readable identifier such as <c>tenant.slug_invalid</c>. Clients may rely on it.</param>
/// <param name="Description">Human-readable explanation for developers and logs. Never echoes user input.</param>
/// <param name="Type">Category that lets outer layers translate the error (e.g. to an HTTP status) without parsing codes.</param>
/// <remarks>Not sealed: specialized errors (such as a validation error carrying field details) derive from it.</remarks>
public record Error(string Code, string Description, ErrorType Type)
{
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
}
