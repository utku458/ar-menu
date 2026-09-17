using ArMenu.Domain.Common;

namespace ArMenu.Application.Common.Validation;

/// <summary>Input validation failed; carries every problem found, not just the first one.</summary>
public sealed record ValidationError(IReadOnlyList<FieldError> Errors)
    : Error("validation.failed", "One or more validation errors occurred.", ErrorType.Validation);

/// <param name="Field">Path of the offending input, e.g. <c>Name[tr]</c>.</param>
/// <param name="Code">Stable error code, reused from the domain where the rule lives there.</param>
/// <param name="Message">Human-readable explanation.</param>
public sealed record FieldError(string Field, string Code, string Message);
