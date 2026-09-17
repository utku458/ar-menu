using ArMenu.Domain.Common;
using ArMenu.Domain.Media;

namespace ArMenu.Domain.Menus;

/// <summary>
/// The 3D / augmented-reality representation of a menu item, rendered in the browser with Google &lt;model-viewer&gt;.
/// </summary>
public sealed record ArModel
{
    private static readonly string[] PosterExtensions = [".webp", ".avif", ".jpg", ".jpeg", ".png"];

    private ArModel(AssetPath glbPath, AssetPath? sceneViewerGlbPath, AssetPath? usdzPath, AssetPath? posterPath)
    {
        GlbPath = glbPath;
        SceneViewerGlbPath = sceneViewerGlbPath;
        UsdzPath = usdzPath;
        PosterPath = posterPath;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private ArModel()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Binary glTF model for the in-page 3D view and WebXR. Processed models use Meshopt geometry and WebP textures,
    /// which three.js decodes but Android Scene Viewer does not.
    /// </summary>
    public AssetPath GlbPath { get; private init; }

    /// <summary>
    /// Plain binary glTF (no compression extensions) for Android Scene Viewer, which opens the file itself. Optional:
    /// without it, devices that need Scene Viewer get <see cref="GlbPath"/>.
    /// </summary>
    public AssetPath? SceneViewerGlbPath { get; private init; }

    /// <summary>
    /// Apple USDZ model for iOS AR Quick Look. Optional: &lt;model-viewer&gt; can convert the GLB on the device,
    /// but a pre-built USDZ opens faster and renders more faithfully.
    /// </summary>
    public AssetPath? UsdzPath { get; private init; }

    /// <summary>
    /// Lightweight still image rendered from the model's initial camera angle. It is painted immediately while the
    /// model streams in, which is what makes AR items feel instant on mobile networks.
    /// </summary>
    public AssetPath? PosterPath { get; private init; }

    public static Result<ArModel> Create(
        AssetPath glbPath,
        AssetPath? sceneViewerGlbPath = null,
        AssetPath? usdzPath = null,
        AssetPath? posterPath = null)
    {
        ArgumentNullException.ThrowIfNull(glbPath);

        if (!glbPath.HasExtension(".glb") || sceneViewerGlbPath?.HasExtension(".glb") == false)
        {
            return MenuItemErrors.ArModelGlbFormatInvalid;
        }

        if (usdzPath is not null && !usdzPath.HasExtension(".usdz"))
        {
            return MenuItemErrors.ArModelUsdzFormatInvalid;
        }

        if (posterPath is not null && !Array.Exists(PosterExtensions, posterPath.HasExtension))
        {
            return MenuItemErrors.ArModelPosterFormatInvalid;
        }

        return new ArModel(glbPath, sceneViewerGlbPath, usdzPath, posterPath);
    }
}
