using ArMenu.Domain.Media;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Media;

public sealed class AssetPathTests
{
    [Theory]
    [InlineData("burger.glb")]
    [InlineData("tenants/0198a1f2/models/smash-burger_v2.glb")]
    [InlineData("demo/posters/Sea.Bass.webp")]
    public void Create_accepts_relative_storage_keys(string path)
    {
        AssetPath.Create(path).ShouldSucceed().Value.ShouldBe(path);
    }

    [Theory]
    [InlineData("/absolute/model.glb")]
    [InlineData("../outside/model.glb")]
    [InlineData("models/../../secret.glb")]
    [InlineData("https://evil.example/model.glb")]
    [InlineData(@"models\model.glb")]
    [InlineData("models//model.glb")]
    [InlineData(".hidden/model.glb")]
    [InlineData("models/model.glb/")]
    [InlineData("models/my model.glb")]
    public void Create_rejects_anything_that_is_not_a_plain_relative_key(string path)
    {
        AssetPath.Create(path).ShouldFailWith(MediaErrors.AssetPathInvalid);
    }

    [Fact]
    public void Create_rejects_keys_longer_than_the_column()
    {
        AssetPath.Create(new string('a', AssetPath.MaxLength + 1)).ShouldFailWith(MediaErrors.AssetPathInvalid);
    }

    [Fact]
    public void HasExtension_ignores_case()
    {
        Make.Asset("models/Burger.GLB").HasExtension(".glb").ShouldBeTrue();
    }
}
