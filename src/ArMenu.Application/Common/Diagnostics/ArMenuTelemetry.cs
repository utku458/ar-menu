using System.Diagnostics;

namespace ArMenu.Application.Common.Diagnostics;

/// <summary>
/// Names shared by the application's traces and metrics. The host exports them (OpenTelemetry); the layers that record
/// them depend only on <see cref="System.Diagnostics"/>.
/// </summary>
public static class ArMenuTelemetry
{
    /// <summary>Name of the <see cref="ActivitySource"/> and the meter.</summary>
    public const string Name = "ArMenu";

    public static readonly ActivitySource ActivitySource = new(Name);

    /// <summary>Tag names, following OpenTelemetry's semantic conventions where one exists.</summary>
    public static class Tags
    {
        public const string UseCase = "armenu.use_case";
        public const string Outcome = "armenu.outcome";
        public const string ErrorType = "error.type";
        public const string AssetLocation = "armenu.asset.location";
        public const string EmailTemplate = "armenu.email.template";
        public const string MenuEvent = "armenu.menu.event";
    }
}
