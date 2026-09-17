using ArMenu.Domain.Menus;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Menus;

public sealed class ArModelTests
{
    [Fact]
    public void Create_accepts_a_glb_with_optional_scene_viewer_glb_usdz_and_poster()
    {
        var model = ArModel.Create(
            Make.Asset("models/burger.glb"),
            Make.Asset("models/burger.scene-viewer.glb"),
            Make.Asset("models/burger.usdz"),
            Make.Asset("posters/burger.webp")).ShouldSucceed();

        model.GlbPath.Value.ShouldBe("models/burger.glb");
        model.SceneViewerGlbPath!.Value.ShouldBe("models/burger.scene-viewer.glb");
        model.UsdzPath!.Value.ShouldBe("models/burger.usdz");
        model.PosterPath!.Value.ShouldBe("posters/burger.webp");
    }

    [Fact]
    public void Create_requires_binary_gltf_for_the_main_model()
    {
        ArModel.Create(Make.Asset("models/burger.gltf")).ShouldFailWith(MenuItemErrors.ArModelGlbFormatInvalid);
    }

    [Fact]
    public void Create_requires_binary_gltf_for_the_scene_viewer_model()
    {
        ArModel.Create(Make.Asset("models/burger.glb"), sceneViewerGlbPath: Make.Asset("models/burger.gltf"))
            .ShouldFailWith(MenuItemErrors.ArModelGlbFormatInvalid);
    }

    [Fact]
    public void Create_requires_usdz_for_the_ios_model()
    {
        ArModel.Create(Make.Asset("models/burger.glb"), usdzPath: Make.Asset("models/burger.reality"))
            .ShouldFailWith(MenuItemErrors.ArModelUsdzFormatInvalid);
    }

    [Theory]
    [InlineData("posters/burger.webp")]
    [InlineData("posters/burger.avif")]
    [InlineData("posters/burger.jpg")]
    [InlineData("posters/burger.jpeg")]
    [InlineData("posters/burger.png")]
    public void Create_accepts_web_image_posters(string poster)
    {
        ArModel.Create(Make.Asset("models/burger.glb"), posterPath: Make.Asset(poster)).ShouldSucceed();
    }

    [Fact]
    public void Create_rejects_posters_browsers_cannot_display()
    {
        ArModel.Create(Make.Asset("models/burger.glb"), posterPath: Make.Asset("posters/burger.tiff"))
            .ShouldFailWith(MenuItemErrors.ArModelPosterFormatInvalid);
    }
}
