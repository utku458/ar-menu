using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Statistics.Queries.GetMenuStatistics;

/// <summary>Guest activity of the tenant bound to the current scope over the last <see cref="Days"/> days, today included.</summary>
public sealed record GetMenuStatisticsQuery(int Days) : IQuery<Result<MenuStatisticsResponse>>;

public sealed record MenuStatisticsResponse(
    DateOnly From,
    DateOnly To,
    MenuStatisticsTotals Totals,
    IReadOnlyList<DailyMenuStatistics> Days,
    IReadOnlyList<DishStatistics> Dishes);

public sealed record MenuStatisticsTotals(long MenuViews, long DishOpens, long ModelViews, long ArStarts);

public sealed record DailyMenuStatistics(DateOnly Day, long MenuViews, long DishOpens, long ArStarts);

/// <summary>A dish's activity, most opened first. Deleted dishes are left out.</summary>
public sealed record DishStatistics(Guid ItemId, string Name, long Opens, long ModelViews, long ArStarts);
