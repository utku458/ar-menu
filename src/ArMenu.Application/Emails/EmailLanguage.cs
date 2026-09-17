namespace ArMenu.Application.Emails;

/// <summary>E-mails are written in Turkish or English, the dashboard's interface languages.</summary>
public static class EmailLanguage
{
    public const string Turkish = "tr";
    public const string English = "en";

    public static readonly IReadOnlyList<string> Supported = [Turkish, English];

    public static bool IsSupported(string? language) => language is not null && Supported.Contains(language);

    /// <summary>The requested language when supported, otherwise Turkish for Turkish businesses and English for others.</summary>
    public static string Resolve(string? requested, string? fallbackCulture) =>
        IsSupported(requested) ? requested! : fallbackCulture == Turkish ? Turkish : English;
}
