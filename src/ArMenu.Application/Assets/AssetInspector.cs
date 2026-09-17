using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using ArMenu.Domain.Common;

namespace ArMenu.Application.Assets;

/// <summary>Reads <paramref name="count"/> bytes of a file, starting at <paramref name="offset"/>.</summary>
public delegate Task<byte[]> ReadRange(long offset, int count, CancellationToken cancellationToken);

/// <summary>Who will open a binary glTF, which decides the extensions it may require.</summary>
public enum GlbProfile
{
    /// <summary>An upload the asset pipeline will process.</summary>
    Source = 1,

    /// <summary>The in-page viewer and WebXR (three.js).</summary>
    Web = 2,

    /// <summary>Android Scene Viewer.</summary>
    SceneViewer = 3,
}

/// <summary>What an accepted file really is, regardless of what the uploader declared.</summary>
public sealed record InspectedAsset(string ContentType, string Extension);

/// <summary>
/// Checks that an uploaded file is what its kind promises, from its bytes rather than its name or declared type.
/// Only the few bytes needed are read, so inspecting a large model stays cheap.
/// </summary>
public static class AssetInspector
{
    /// <summary>A glTF JSON chunk describes the scene; megabytes of it would be a broken or hostile file.</summary>
    public const int MaxModelJsonBytes = 4 * 1024 * 1024;

    private const int GlbHeaderLength = 20;
    private const uint GlbMagic = 0x46546C67; // "glTF"
    private const uint GlbVersion = 2;
    private const uint JsonChunkType = 0x4E4F534A; // "JSON"
    private const int ZipLocalHeaderLength = 30;
    private const uint ZipLocalHeaderSignature = 0x04034B50;
    private const int AppleModelHeadLength = 512;
    private const int ImageHeadLength = 16;

    // Required extensions three.js decodes without downloading anything: the in-page viewer and WebXR.
    private static readonly FrozenSet<string> WebRequiredExtensions = FrozenSet.Create(
        StringComparer.Ordinal,
        "EXT_meshopt_compression",
        "KHR_mesh_quantization",
        "KHR_texture_transform",
        "KHR_materials_unlit",
        "KHR_materials_emissive_strength",
        "KHR_materials_clearcoat",
        "KHR_materials_transmission",
        "KHR_materials_volume",
        "KHR_materials_ior",
        "KHR_materials_specular",
        "KHR_materials_sheen",
        "KHR_lights_punctual",
        "EXT_texture_webp",
        "EXT_texture_avif");

    // What the asset pipeline can read: everything above, plus Draco geometry, which it decodes. KTX2 textures stay out:
    // the pipeline cannot turn them into the PNG/JPEG textures Scene Viewer and AR Quick Look need.
    private static readonly FrozenSet<string> SourceRequiredExtensions =
        WebRequiredExtensions.Append("KHR_draco_mesh_compression").ToFrozenSet(StringComparer.Ordinal);

    // Google documents only these two for Scene Viewer, which opens the file itself.
    private static readonly FrozenSet<string> SceneViewerRequiredExtensions = FrozenSet.Create(
        StringComparer.Ordinal,
        "KHR_texture_transform",
        "KHR_materials_unlit");

    public static async Task<Result<InspectedAsset>> InspectAsync(
        AssetKind kind,
        long size,
        ReadRange read,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(read);

        return kind switch
        {
            AssetKind.Model => await InspectModelAsync(GlbProfile.Source, size, read, cancellationToken),
            AssetKind.AppleModel => InspectAppleModel(await ReadHeadAsync(size, AppleModelHeadLength, read, cancellationToken)),
            AssetKind.Poster or AssetKind.Logo =>
                InspectImage(await ReadHeadAsync(size, ImageHeadLength, read, cancellationToken), AssetErrors.Invalid(kind)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown asset kind."),
        };
    }

    /// <summary>Checks a binary glTF against what the consumer of <paramref name="profile"/> can decode.</summary>
    public static async Task<Result<InspectedAsset>> InspectModelAsync(
        GlbProfile profile,
        long size,
        ReadRange read,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(read);

        if (size < GlbHeaderLength)
        {
            return AssetErrors.ModelInvalid;
        }

        // GLB layout (little-endian): magic, version, total length, then the JSON chunk's length and type.
        var header = await read(0, GlbHeaderLength, cancellationToken);
        if (header.Length < GlbHeaderLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(header) != GlbMagic ||
            BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4)) != GlbVersion ||
            BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8)) != size ||
            BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(16)) != JsonChunkType)
        {
            return AssetErrors.ModelInvalid;
        }

        var jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
        if (jsonLength is 0 or > MaxModelJsonBytes || GlbHeaderLength + jsonLength > size)
        {
            return AssetErrors.ModelInvalid;
        }

        var allowed = profile switch
        {
            GlbProfile.Source => SourceRequiredExtensions,
            GlbProfile.Web => WebRequiredExtensions,
            GlbProfile.SceneViewer => SceneViewerRequiredExtensions,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown GLB profile."),
        };

        return InspectGltfJson(await read(GlbHeaderLength, (int)jsonLength, cancellationToken), allowed);
    }

    private static Result<InspectedAsset> InspectGltfJson(byte[] json, FrozenSet<string> allowedRequiredExtensions)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("asset", out var asset) ||
                asset.ValueKind != JsonValueKind.Object ||
                !asset.TryGetProperty("version", out var version) ||
                version.ValueKind != JsonValueKind.String ||
                version.GetString() != "2.0")
            {
                return AssetErrors.ModelInvalid;
            }

            if (root.TryGetProperty("extensionsRequired", out var required))
            {
                if (required.ValueKind != JsonValueKind.Array)
                {
                    return AssetErrors.ModelInvalid;
                }

                foreach (var extension in required.EnumerateArray())
                {
                    if (extension.ValueKind != JsonValueKind.String || !allowedRequiredExtensions.Contains(extension.GetString()!))
                    {
                        return AssetErrors.ModelExtensionUnsupported;
                    }
                }
            }

            return new InspectedAsset(AssetLimits.ModelContentType, ".glb");
        }
        catch (JsonException)
        {
            return AssetErrors.ModelInvalid;
        }
    }

    // USDZ is a zip archive whose entries are stored uncompressed, and whose first entry is the root USD layer.
    private static Result<InspectedAsset> InspectAppleModel(ReadOnlySpan<byte> head)
    {
        if (head.Length < ZipLocalHeaderLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(head) != ZipLocalHeaderSignature ||
            BinaryPrimitives.ReadUInt16LittleEndian(head[8..]) != 0)
        {
            return AssetErrors.AppleModelInvalid;
        }

        var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(head[26..]);
        if (ZipLocalHeaderLength + nameLength > head.Length)
        {
            return AssetErrors.AppleModelInvalid;
        }

        var firstEntry = Encoding.ASCII.GetString(head.Slice(ZipLocalHeaderLength, nameLength));
        return firstEntry.EndsWith(".usdc", StringComparison.OrdinalIgnoreCase) ||
               firstEntry.EndsWith(".usda", StringComparison.OrdinalIgnoreCase) ||
               firstEntry.EndsWith(".usd", StringComparison.OrdinalIgnoreCase)
            ? new InspectedAsset(AssetLimits.AppleModelContentType, ".usdz")
            : AssetErrors.AppleModelInvalid;
    }

    /// <summary>
    /// Accepts the four raster formats every current browser decodes, recognised by their own bytes. An SVG would be
    /// a document with scripting of its own, so nothing here can ever report one.
    /// </summary>
    private static Result<InspectedAsset> InspectImage(ReadOnlySpan<byte> head, Error invalid)
    {
        if (head.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return new InspectedAsset("image/png", ".png");
        }

        if (head.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return new InspectedAsset("image/jpeg", ".jpg");
        }

        if (head.Length >= 12 && head.StartsWith("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8))
        {
            return new InspectedAsset("image/webp", ".webp");
        }

        if (head.Length >= 12 && head[4..8].SequenceEqual("ftyp"u8) &&
            (head[8..12].SequenceEqual("avif"u8) || head[8..12].SequenceEqual("avis"u8)))
        {
            return new InspectedAsset("image/avif", ".avif");
        }

        return invalid;
    }

    private static Task<byte[]> ReadHeadAsync(long size, int length, ReadRange read, CancellationToken cancellationToken) =>
        size <= 0 ? Task.FromResult(Array.Empty<byte>()) : read(0, (int)Math.Min(size, length), cancellationToken);
}
