namespace ArMenu.Application.Emails;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    /// <summary>Root of the dashboard, such as <c>https://app.armenu.app/</c>. Links in e-mails open its pages.</summary>
    public Uri? Url { get; set; }
}
