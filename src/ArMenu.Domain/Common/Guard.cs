using System.Runtime.CompilerServices;

namespace ArMenu.Domain.Common;

/// <summary>Argument checks for programming errors that the BCL throw helpers do not cover.</summary>
internal static class Guard
{
    public static void NotDefault<T>(T value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : struct, IEquatable<T>
    {
        if (value.Equals(default))
        {
            throw new ArgumentException("The value cannot be the default value of its type.", parameterName);
        }
    }
}
