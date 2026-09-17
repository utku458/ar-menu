using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ArMenu.Application.Assets;

namespace ArMenu.Application.UnitTests.Assets;

public sealed class AssetInspectorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_binary_gltf_2_model_is_accepted()
    {
        var result = await InspectAsync(AssetKind.Model, Glb("""{"asset":{"version":"2.0"},"extensionsRequired":["KHR_mesh_quantization"]}"""));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new InspectedAsset("model/gltf-binary", ".glb"));
    }

    // Who opens the file decides what it may require: the pipeline decodes Draco, three.js decodes Meshopt and WebP,
    // Scene Viewer decodes neither, and nothing turns KTX2 textures into the PNG or JPEG that AR apps need.
    [Theory]
    [InlineData(GlbProfile.Source, "KHR_draco_mesh_compression", true)]
    [InlineData(GlbProfile.Source, "EXT_meshopt_compression", true)]
    [InlineData(GlbProfile.Source, "KHR_texture_basisu", false)]
    [InlineData(GlbProfile.Web, "EXT_meshopt_compression", true)]
    [InlineData(GlbProfile.Web, "EXT_texture_webp", true)]
    [InlineData(GlbProfile.Web, "KHR_draco_mesh_compression", false)]
    [InlineData(GlbProfile.SceneViewer, "KHR_texture_transform", true)]
    [InlineData(GlbProfile.SceneViewer, "KHR_mesh_quantization", false)]
    [InlineData(GlbProfile.SceneViewer, "EXT_meshopt_compression", false)]
    [InlineData(GlbProfile.SceneViewer, "EXT_texture_webp", false)]
    public async Task Required_extensions_are_checked_against_what_the_consumer_decodes(GlbProfile profile, string extension, bool accepted)
    {
        var file = Glb($$"""{"asset":{"version":"2.0"},"extensionsRequired":["{{extension}}"]}""");

        var result = await AssetInspector.InspectModelAsync(
            profile,
            file.Length,
            (offset, count, _) => Task.FromResult(file.Skip((int)offset).Take(count).ToArray()),
            Ct);

        if (accepted)
        {
            result.IsSuccess.ShouldBeTrue();
        }
        else
        {
            result.Error.ShouldBe(AssetErrors.ModelExtensionUnsupported);
        }
    }

    public static TheoryData<string, byte[]> BrokenModels => new()
    {
        { "not a glTF at all", "PK this is a zip"u8.ToArray() },
        { "glTF 1.0 container", WithUInt32(Glb("""{"asset":{"version":"2.0"}}"""), offset: 4, value: 1) },
        { "length header does not match the file", WithUInt32(Glb("""{"asset":{"version":"2.0"}}"""), offset: 8, value: 9999) },
        { "JSON chunk longer than the file", WithUInt32(Glb("""{"asset":{"version":"2.0"}}"""), offset: 12, value: 1_000_000) },
        { "malformed JSON", Glb("""{"asset":""") },
        { "glTF 1.0 JSON", Glb("""{"asset":{"version":"1.0"}}""") },
    };

    [Theory]
    [MemberData(nameof(BrokenModels))]
    public async Task Files_that_only_look_like_models_are_rejected(string because, byte[] file)
    {
        var result = await InspectAsync(AssetKind.Model, file);

        result.Error.ShouldBe(AssetErrors.ModelInvalid, because);
    }

    [Fact]
    public async Task A_usdz_package_with_a_stored_root_layer_is_accepted()
    {
        var result = await InspectAsync(AssetKind.AppleModel, Zip("model.usda", CompressionLevel.NoCompression));

        result.Value.ShouldBe(new InspectedAsset("model/vnd.usdz+zip", ".usdz"));
    }

    [Theory]
    [InlineData("model.usda", CompressionLevel.Optimal)]
    [InlineData("readme.txt", CompressionLevel.NoCompression)]
    public async Task Zip_files_that_are_not_valid_usdz_packages_are_rejected(string firstEntry, CompressionLevel compression)
    {
        var result = await InspectAsync(AssetKind.AppleModel, Zip(firstEntry, compression));

        result.Error.ShouldBe(AssetErrors.AppleModelInvalid);
    }

    public static TheoryData<byte[], string> Posters => new()
    {
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13], "image/png" },
        { [0xFF, 0xD8, 0xFF, 0xE0, 0, 16, 0x4A, 0x46, 0x49, 0x46, 0, 1], "image/jpeg" },
        { "RIFF$\0\0\0WEBPVP8 "u8.ToArray(), "image/webp" },
        { "\0\0\0ftypavif\0\0\0\0"u8.ToArray(), "image/avif" },
    };

    [Theory]
    [MemberData(nameof(Posters))]
    public async Task Posters_are_recognized_by_their_signature(byte[] file, string contentType)
    {
        var result = await InspectAsync(AssetKind.Poster, file);

        result.Value.ContentType.ShouldBe(contentType);
    }

    [Fact]
    public async Task A_renamed_file_is_not_a_poster()
    {
        var result = await InspectAsync(AssetKind.Poster, "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray());

        result.Error.ShouldBe(AssetErrors.PosterInvalid);
    }

    private static Task<Domain.Common.Result<InspectedAsset>> InspectAsync(AssetKind kind, byte[] file) =>
        AssetInspector.InspectAsync(
            kind,
            file.Length,
            (offset, count, _) => Task.FromResult(file.Skip((int)offset).Take(count).ToArray()),
            Ct);

    /// <summary>A GLB with only a JSON chunk, padded to four bytes with spaces as the specification requires.</summary>
    private static byte[] Glb(string json)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var padded = jsonBytes.Concat(Enumerable.Repeat((byte)' ', (4 - (jsonBytes.Length % 4)) % 4)).ToArray();
        var file = new byte[20 + padded.Length];

        BinaryPrimitives.WriteUInt32LittleEndian(file, 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)file.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)padded.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(16), 0x4E4F534A);
        padded.CopyTo(file, 20);

        return file;
    }

    private static byte[] WithUInt32(byte[] file, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(offset), value);
        return file;
    }

    private static byte[] Zip(string firstEntry, CompressionLevel compression)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry(firstEntry, compression).Open());
            writer.Write(string.Concat(Enumerable.Repeat("#usda 1.0\n", 50)));
        }

        return stream.ToArray();
    }
}
