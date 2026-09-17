using System.Collections.Frozen;

namespace ArMenu.Application.ArModels;

/// <summary>
/// The rejection codes of the asset pipeline (web/packages/model-pipeline/src/rejection.ts). They are shown to the
/// business, so an unknown code from a newer processor is reported as a generic failure rather than passed through.
/// </summary>
public static class ModelRejections
{
    public static readonly FrozenSet<string> Known = FrozenSet.Create(
        StringComparer.Ordinal,
        "model.too_large",
        "model.unreadable",
        "model.empty",
        "model.too_complex",
        "model.texture_unsupported",
        "model.render_failed");
}
