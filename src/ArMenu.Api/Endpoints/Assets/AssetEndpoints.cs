using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Application.Assets;
using ArMenu.Application.Assets.CreateAssetUpload;
using ArMenu.Application.Assets.PublishAssetUpload;
using Mediator;

namespace ArMenu.Api.Endpoints.Assets;

/// <summary>
/// Two-step uploads of 3D models and posters. Files never pass through the API: browsers upload them straight to
/// storage with a presigned request, then ask the API to inspect and publish what arrived.
/// </summary>
internal sealed class AssetEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var assets = endpoints.MapGroup("/api/v1/manage/assets")
            .RequireAuthorization(AuthorizationPolicies.MenuEditor)
            .RequireTenantFromClaims()
            .WithTags("Assets")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        assets.MapPost("/uploads", CreateUploadAsync)
            .WithSummary("Grants a short-lived upload of exactly one file, of the declared type and size, straight to storage.")
            .Produces<AssetUpload>();

        assets.MapPost("/uploads/{uploadId:guid}/publish", PublishAsync)
            .WithSummary("Inspects an uploaded file and publishes it under an immutable public URL, ready to attach to an item.")
            .Produces<PublishedAsset>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateUploadAsync(UploadRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateAssetUploadCommand(request.Kind, request.ContentType, request.Size), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> PublishAsync(Guid uploadId, PublishRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAssetUploadCommand(uploadId, request.Kind), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    internal sealed record UploadRequest(AssetKind Kind, string ContentType, long Size);

    internal sealed record PublishRequest(AssetKind Kind);
}
