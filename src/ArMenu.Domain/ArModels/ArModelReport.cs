namespace ArMenu.Domain.ArModels;

/// <summary>What processing found in an upload and made of it, shown to the business next to the model.</summary>
public sealed record ArModelReport
{
    public ArModelReport(
        ModelStatistics source,
        ModelStatistics optimized,
        ModelFileSizes files,
        ModelDimensions dimensions,
        IReadOnlyList<string> warnings)
    {
        Source = source;
        Optimized = optimized;
        Files = files;
        Dimensions = dimensions;
        Warnings = warnings;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private ArModelReport()
    {
    }
#pragma warning restore CS8618

    public ModelStatistics Source { get; private init; }

    public ModelStatistics Optimized { get; private init; }

    public ModelFileSizes Files { get; private init; }

    public ModelDimensions Dimensions { get; private init; }

    /// <summary>Codes of things worth a look that did not stop publishing, such as <c>model.simplified</c>.</summary>
    public IReadOnlyList<string> Warnings { get; private init; }
}

public sealed record ModelStatistics(int Triangles, int Vertices, int Materials, int Textures, int MaxTextureSize);

/// <summary>Sizes in bytes of the uploaded source and of every published file.</summary>
public sealed record ModelFileSizes(long Source, long Model, long SceneViewerModel, long AppleModel, long Poster);

/// <summary>Bounding box in meters: the size a guest sees on the table in AR.</summary>
public sealed record ModelDimensions(double Width, double Height, double Depth);
