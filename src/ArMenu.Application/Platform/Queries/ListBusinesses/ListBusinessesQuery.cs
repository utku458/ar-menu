using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Platform.Queries.ListBusinesses;

/// <summary>
/// Every business on the platform, for the administrator to open or manage. Only what the business table itself
/// holds: who works where lives in memberships, behind each business's row-level security, and the administrator
/// sees it by entering the business rather than by reading across all of them.
/// </summary>
public sealed record ListBusinessesQuery : IQuery<Result<IReadOnlyList<BusinessSummary>>>;

public sealed record BusinessSummary(Guid Id, string Name, string Slug, string Status, DateTimeOffset CreatedAt);
