using System.Text;
using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Application.ArModels.Queries.GetArModelProcessing;
using ArMenu.Application.ArModels.StartArModelProcessing;
using ArMenu.Application.Menus.Categories.CreateMenuCategory;
using ArMenu.Application.Menus.Categories.DeleteMenuCategory;
using ArMenu.Application.Menus.Categories.ReorderMenuCategories;
using ArMenu.Application.Menus.Categories.UpdateMenuCategory;
using ArMenu.Application.Menus.Items.AttachMenuItemArModel;
using ArMenu.Application.Menus.Items.CreateMenuItem;
using ArMenu.Application.Menus.Items.DeleteMenuItem;
using ArMenu.Application.Menus.Items.DetachMenuItemArModel;
using ArMenu.Application.Menus.Items.ReorderMenuItems;
using ArMenu.Application.Menus.Items.SetMenuItemAvailability;
using ArMenu.Application.Menus.Items.UpdateMenuItem;
using ArMenu.Application.Menus.Queries.GetManagedMenu;
using ArMenu.Application.Menus.Transfer.ImportMenu;
using ArMenu.Application.Menus.Transfer.Queries.ExportMenu;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Api.Endpoints.Menus;

/// <summary>
/// Menu management for staff. The tenant always comes from the access token, never from the URL or body,
/// so a member of one business cannot address another business's menu at all.
/// </summary>
internal sealed class ManageMenuEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var menu = endpoints.MapGroup("/api/v1/manage/menu")
            .RequireAuthorization(AuthorizationPolicies.MenuStaff)
            .RequireTenantFromClaims()
            .WithTags("Menu management")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        menu.MapGet("/", GetMenuAsync).Produces<ManagedMenuResponse>();
        menu.MapPut("/items/{itemId:guid}/availability", SetAvailabilityAsync).Produces(StatusCodes.Status204NoContent);

        var editor = menu.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.MenuEditor)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        editor.MapPost("/categories", CreateCategoryAsync).Produces<CreatedResponse>(StatusCodes.Status201Created);
        editor.MapPut("/categories/order", ReorderCategoriesAsync).Produces(StatusCodes.Status204NoContent);
        editor.MapPut("/categories/{categoryId:guid}", UpdateCategoryAsync).Produces(StatusCodes.Status204NoContent);
        editor.MapDelete("/categories/{categoryId:guid}", DeleteCategoryAsync)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);
        editor.MapPut("/categories/{categoryId:guid}/items/order", ReorderItemsAsync).Produces(StatusCodes.Status204NoContent);

        editor.MapPost("/items", CreateItemAsync).Produces<CreatedResponse>(StatusCodes.Status201Created);
        editor.MapPut("/items/{itemId:guid}", UpdateItemAsync).Produces(StatusCodes.Status204NoContent);
        editor.MapDelete("/items/{itemId:guid}", DeleteItemAsync).Produces(StatusCodes.Status204NoContent);
        editor.MapPut("/items/{itemId:guid}/ar-model", AttachArModelAsync).Produces(StatusCodes.Status204NoContent);
        editor.MapDelete("/items/{itemId:guid}/ar-model", DetachArModelAsync).Produces(StatusCodes.Status204NoContent);

        editor.MapGet("/export", ExportAsync)
            .WithSummary("The whole menu as a CSV spreadsheet (UTF-8), one row per dish and one column per language.")
            .Produces<string>(StatusCodes.Status200OK, "text/csv");
        editor.MapPost("/import", ImportAsync)
            .Accepts<string>("text/csv")
            .WithSummary(
                "Applies a menu spreadsheet in the export's format: rows with an id update that dish, rows without one add a " +
                "dish. All or nothing; with dryRun=true only reports what would change.")
            .Produces<MenuImportResponse>()
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        editor.MapPost("/items/{itemId:guid}/ar-model/processing", StartArModelProcessingAsync)
            .WithSummary(
                "Turns an uploaded GLB into the item's 3D model: an optimized GLB, a Scene Viewer GLB, a USDZ and a poster. " +
                "The item keeps its current model until processing succeeds.")
            .Produces<ArModelProcessingResponse>(StatusCodes.Status202Accepted);
        editor.MapGet("/items/{itemId:guid}/ar-model/processing", GetArModelProcessingAsync)
            .WithName(GetProcessingRouteName)
            .WithSummary("The latest model processing of the item: its progress, or its report once finished.")
            .Produces<ArModelProcessingResponse>();
    }

    private const string GetProcessingRouteName = "GetArModelProcessing";

    private static async Task<IResult> ExportAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ExportMenuQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.File(Encoding.UTF8.GetBytes(result.Value.Csv), "text/csv; charset=utf-8", result.Value.FileName)
            : result.Error.ToProblem();
    }

    // The body is read as text up to the limit; a larger file is refused before it is buffered whole.
    private static async Task<IResult> ImportAsync(HttpRequest request, bool? dryRun, IMediator mediator, CancellationToken cancellationToken)
    {
        if (request.ContentType is not { } contentType || !contentType.StartsWith("text/csv", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status415UnsupportedMediaType, detail: "Send the file as text/csv.", extensions: new Dictionary<string, object?> { ["code"] = "menu_import.unsupported_media_type" });
        }

        if (request.ContentLength > ImportMenuCommand.MaxBytes)
        {
            return TooLarge();
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > ImportMenuCommand.MaxBytes)
            {
                return TooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        var csv = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        var result = await mediator.Send(new ImportMenuCommand(csv, dryRun ?? false), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();

        static IResult TooLarge() => TypedResults.Problem(
            statusCode: StatusCodes.Status413PayloadTooLarge,
            detail: "Menu files can be at most 1 MiB.",
            extensions: new Dictionary<string, object?> { ["code"] = "menu_import.too_large" });
    }

    private static async Task<IResult> GetMenuAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetManagedMenuQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> CreateCategoryAsync(CategoryRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateMenuCategoryCommand(request.Name, request.Description), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/manage/menu/categories/{result.Value}", new CreatedResponse(result.Value.Value))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateCategoryAsync(Guid categoryId, CategoryRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(
            new UpdateMenuCategoryCommand(MenuCategoryId.From(categoryId), request.Name, request.Description, request.IsVisible),
            cancellationToken)).ToNoContent();

    private static async Task<IResult> DeleteCategoryAsync(Guid categoryId, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new DeleteMenuCategoryCommand(MenuCategoryId.From(categoryId)), cancellationToken)).ToNoContent();

    private static async Task<IResult> ReorderCategoriesAsync(ReorderRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(
            new ReorderMenuCategoriesCommand([.. request.Ids.Select(MenuCategoryId.From)]),
            cancellationToken)).ToNoContent();

    private static async Task<IResult> ReorderItemsAsync(Guid categoryId, ReorderRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(
            new ReorderMenuItemsCommand(MenuCategoryId.From(categoryId), [.. request.Ids.Select(MenuItemId.From)]),
            cancellationToken)).ToNoContent();

    private static async Task<IResult> CreateItemAsync(CreateItemRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMenuItemCommand(
                MenuCategoryId.From(request.CategoryId),
                request.Name,
                request.Description,
                request.Price,
                request.Allergens,
                request.DietaryLabels),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/manage/menu/items/{result.Value}", new CreatedResponse(result.Value.Value))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateItemAsync(Guid itemId, UpdateItemRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(
            new UpdateMenuItemCommand(
                MenuItemId.From(itemId),
                MenuCategoryId.From(request.CategoryId),
                request.Name,
                request.Description,
                request.Price,
                request.IsVisible,
                request.Allergens,
                request.DietaryLabels),
            cancellationToken)).ToNoContent();

    private static async Task<IResult> SetAvailabilityAsync(Guid itemId, AvailabilityRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new SetMenuItemAvailabilityCommand(MenuItemId.From(itemId), request.IsAvailable), cancellationToken)).ToNoContent();

    private static async Task<IResult> DeleteItemAsync(Guid itemId, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new DeleteMenuItemCommand(MenuItemId.From(itemId)), cancellationToken)).ToNoContent();

    private static async Task<IResult> AttachArModelAsync(Guid itemId, ArModelRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(
            new AttachMenuItemArModelCommand(
                MenuItemId.From(itemId),
                request.GlbPath,
                request.SceneViewerGlbPath,
                request.UsdzPath,
                request.PosterPath),
            cancellationToken)).ToNoContent();

    private static async Task<IResult> StartArModelProcessingAsync(
        Guid itemId,
        ArModelProcessingRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var started = await mediator.Send(new StartArModelProcessingCommand(MenuItemId.From(itemId), request.UploadId), cancellationToken);
        if (started.IsFailure)
        {
            return started.Error.ToProblem();
        }

        var processing = await mediator.Send(new GetArModelProcessingQuery(MenuItemId.From(itemId)), cancellationToken);
        return processing.IsSuccess
            ? TypedResults.AcceptedAtRoute(processing.Value, GetProcessingRouteName, new { itemId })
            : processing.Error.ToProblem();
    }

    private static async Task<IResult> GetArModelProcessingAsync(Guid itemId, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetArModelProcessingQuery(MenuItemId.From(itemId)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> DetachArModelAsync(Guid itemId, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new DetachMenuItemArModelCommand(MenuItemId.From(itemId)), cancellationToken)).ToNoContent();

    internal sealed record CategoryRequest(
        Dictionary<string, string> Name,
        Dictionary<string, string>? Description,
        bool IsVisible = true);

    /// <summary>
    /// <c>Allergens</c>: codes of the EU's fourteen (<c>gluten</c>, <c>crustaceans</c>, <c>eggs</c>, <c>fish</c>,
    /// <c>peanuts</c>, <c>soybeans</c>, <c>milk</c>, <c>nuts</c>, <c>celery</c>, <c>mustard</c>, <c>sesame</c>,
    /// <c>sulphites</c>, <c>lupin</c>, <c>molluscs</c>); <c>null</c> when not declared, <c>[]</c> for none.
    /// <c>DietaryLabels</c>: <c>vegetarian</c>, <c>vegan</c>, <c>glutenFree</c>.
    /// </summary>
    internal sealed record CreateItemRequest(
        Guid CategoryId,
        Dictionary<string, string> Name,
        Dictionary<string, string>? Description,
        decimal Price,
        List<string>? Allergens = null,
        List<string>? DietaryLabels = null);

    /// <summary>Replaces the item's details; see <see cref="CreateItemRequest"/> for allergens and dietary labels.</summary>
    internal sealed record UpdateItemRequest(
        Guid CategoryId,
        Dictionary<string, string> Name,
        Dictionary<string, string>? Description,
        decimal Price,
        bool IsVisible,
        List<string>? Allergens = null,
        List<string>? DietaryLabels = null);

    internal sealed record AvailabilityRequest(bool IsAvailable);

    internal sealed record ArModelRequest(string GlbPath, string? SceneViewerGlbPath, string? UsdzPath, string? PosterPath);

    internal sealed record ArModelProcessingRequest(Guid UploadId);

    internal sealed record ReorderRequest(IReadOnlyList<Guid> Ids);

    internal sealed record CreatedResponse(Guid Id);
}
