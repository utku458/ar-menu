using ArMenu.Domain.Media;

namespace ArMenu.Application.Abstractions.Assets;

/// <summary>Turns stored asset keys into public (CDN) URLs at read time.</summary>
public interface IAssetUrlResolver
{
    Uri Resolve(AssetPath path);
}
