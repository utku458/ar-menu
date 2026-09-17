using ArMenu.Application.Abstractions.Assets;
using ArMenu.Domain.Media;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

internal sealed class AssetUrlResolver(IOptions<AssetOptions> options) : IAssetUrlResolver
{
    private readonly Uri _baseUrl = options.Value.PublicBaseUrl
        ?? throw new InvalidOperationException($"{AssetOptions.SectionName}:PublicBaseUrl is not configured.");

    public Uri Resolve(AssetPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new Uri(_baseUrl, path.Value);
    }
}
