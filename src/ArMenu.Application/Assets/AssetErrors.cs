using System.Globalization;
using ArMenu.Domain.Common;

namespace ArMenu.Application.Assets;

public static class AssetErrors
{
    public static readonly Error ContentTypeNotAllowed = Error.Validation(
        "asset.content_type_not_allowed", "This kind of asset does not accept files of that type.");

    public static readonly Error SizeInvalid = Error.Validation(
        "asset.size_invalid", "Uploads must declare their size in bytes.");

    public static readonly Error UploadNotFound = Error.NotFound(
        "asset.upload_not_found", "No upload with this id was completed for the business, or it has expired.");

    public static readonly Error ModelInvalid = Error.Validation(
        "asset.model_invalid", "The file is not a valid binary glTF 2.0 (.glb) model.");

    public static readonly Error ModelExtensionUnsupported = Error.Validation(
        "asset.model_extension_unsupported",
        "The model requires a glTF extension the asset pipeline cannot process, such as KTX2 textures. Export it with " +
        "PNG, JPEG or WebP textures.");

    public static readonly Error ModelRequiresProcessing = Error.Validation(
        "asset.model_requires_processing",
        "Models are published through processing, which creates every file guests' devices need.");

    /// <summary>The processor reported success, but a file it produced is not what it should be.</summary>
    public static readonly Error ProcessingOutputInvalid = Error.Validation(
        "asset.processing_output_invalid", "Processing produced an invalid file. Upload the model again.");

    /// <summary>The processor refused the model for a reason this API does not know.</summary>
    public static readonly Error ProcessingFailed = Error.Validation(
        "asset.processing_failed", "The model could not be processed.");

    public static readonly Error AppleModelInvalid = Error.Validation(
        "asset.apple_model_invalid", "The file is not a valid USDZ package.");

    public static readonly Error PosterInvalid = Error.Validation(
        "asset.poster_invalid", "Posters must be WebP, AVIF, PNG or JPEG images.");

    public static readonly Error LogoInvalid = Error.Validation(
        "asset.logo_invalid", "Logos must be WebP, AVIF, PNG or JPEG images.");

    public static readonly Error NotOwned = Error.Validation(
        "asset.not_owned", "Only assets uploaded by this business can be used.");

    public static readonly Error NotFound = Error.Validation(
        "asset.not_found", "The asset does not exist in storage.");

    public static Error TooLarge(AssetKind kind) => Error.Validation(
        "asset.too_large",
        string.Create(CultureInfo.InvariantCulture, $"Files of this kind cannot exceed {AssetLimits.MaxBytes(kind) / (1024 * 1024)} MiB."));

    public static Error Invalid(AssetKind kind) => kind switch
    {
        AssetKind.Model => ModelInvalid,
        AssetKind.AppleModel => AppleModelInvalid,
        AssetKind.Logo => LogoInvalid,
        _ => PosterInvalid,
    };
}
