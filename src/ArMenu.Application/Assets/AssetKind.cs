using System.Text.Json.Serialization;

namespace ArMenu.Application.Assets;

/// <summary>What a browser may upload: the files of a dish's 3D/AR model (see <c>ArModel</c>), or a business's logo.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AssetKind>))]
public enum AssetKind
{
    /// <summary>Binary glTF (<c>.glb</c>): in-page 3D, Android Scene Viewer, WebXR.</summary>
    [JsonStringEnumMemberName("model")]
    Model = 1,

    /// <summary>USDZ for iOS AR Quick Look.</summary>
    [JsonStringEnumMemberName("appleModel")]
    AppleModel = 2,

    /// <summary>Still image shown while the model loads and in the menu list.</summary>
    [JsonStringEnumMemberName("poster")]
    Poster = 3,

    /// <summary>The business's logo, shown at the top of its guest menu.</summary>
    [JsonStringEnumMemberName("logo")]
    Logo = 4,
}
