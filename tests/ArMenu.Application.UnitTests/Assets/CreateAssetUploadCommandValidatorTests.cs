using ArMenu.Application.Assets;
using ArMenu.Application.Assets.CreateAssetUpload;

namespace ArMenu.Application.UnitTests.Assets;

public sealed class CreateAssetUploadCommandValidatorTests
{
    private readonly CreateAssetUploadCommandValidator _validator = new();

    [Theory]
    [InlineData(AssetKind.Model, "model/gltf-binary", AssetLimits.MaxModelBytes)]
    [InlineData(AssetKind.AppleModel, "model/vnd.usdz+zip", 1)]
    [InlineData(AssetKind.Poster, "image/webp", AssetLimits.MaxPosterBytes)]
    [InlineData(AssetKind.Poster, "IMAGE/JPEG", 500)]
    public async Task Uploads_within_their_kind_limits_are_granted(AssetKind kind, string contentType, long size)
    {
        var result = await _validator.ValidateAsync(new CreateAssetUploadCommand(kind, contentType, size), TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(AssetKind.Model, "model/gltf+json", 100, "asset.content_type_not_allowed")]
    [InlineData(AssetKind.Poster, "image/svg+xml", 100, "asset.content_type_not_allowed")]
    [InlineData(AssetKind.Poster, "image/png", AssetLimits.MaxPosterBytes + 1, "asset.too_large")]
    [InlineData(AssetKind.Model, "model/gltf-binary", 0, "asset.size_invalid")]
    public async Task Uploads_outside_their_kind_limits_are_refused(AssetKind kind, string contentType, long size, string code)
    {
        var result = await _validator.ValidateAsync(new CreateAssetUploadCommand(kind, contentType, size), TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.ErrorCode).ShouldContain(code);
    }
}
