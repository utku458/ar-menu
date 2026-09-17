using ArMenu.Application.Menus.Statistics;
using ArMenu.Application.Menus.Statistics.Queries.GetMenuStatistics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Statistics;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Statistics;

public sealed class GetMenuStatisticsQueryHandler(ArMenuDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    : IQueryHandler<GetMenuStatisticsQuery, Result<MenuStatisticsResponse>>
{
    private static readonly string MenuViewed = MenuEventType.MenuViewed.Name();
    private static readonly string DishOpened = MenuEventType.DishOpened.Name();
    private static readonly string ModelViewed = MenuEventType.ModelViewed.Name();
    private static readonly string ArStarted = MenuEventType.ArStarted.Name();

    public async ValueTask<Result<MenuStatisticsResponse>> Handle(GetMenuStatisticsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var to = tenantContext.RequireTenant().DateAt(timeProvider.GetUtcNow());
        var from = to.AddDays(1 - query.Days);

        var counters = await dbContext.Set<MenuDailyStatistic>()
            .AsNoTracking()
            .Where(statistic => statistic.Day >= from && statistic.Day <= to)
            .GroupBy(statistic => new { statistic.Day, statistic.Event, statistic.MenuItemId })
            .Select(group => new { group.Key.Day, group.Key.Event, group.Key.MenuItemId, Count = group.Sum(statistic => statistic.Count) })
            .ToListAsync(cancellationToken);

        long Sum(string eventName, Func<DateOnly, bool>? onDay = null) =>
            counters.Where(counter => counter.Event == eventName && (onDay is null || onDay(counter.Day))).Sum(counter => counter.Count);

        var days = Enumerable.Range(0, query.Days)
            .Select(offset => from.AddDays(offset))
            .Select(day => new DailyMenuStatistics(day, Sum(MenuViewed, d => d == day), Sum(DishOpened, d => d == day), Sum(ArStarted, d => d == day)))
            .ToList();

        var perDish = counters
            .Where(counter => counter.MenuItemId != MenuDailyStatistic.WholeMenu)
            .GroupBy(counter => counter.MenuItemId)
            .ToDictionary(
                group => group.Key,
                group => (
                    Opens: group.Where(counter => counter.Event == DishOpened).Sum(counter => counter.Count),
                    ModelViews: group.Where(counter => counter.Event == ModelViewed).Sum(counter => counter.Count),
                    ArStarts: group.Where(counter => counter.Event == ArStarted).Sum(counter => counter.Count)));

        var itemIds = perDish.Keys.Select(Domain.Menus.MenuItemId.From).ToList();
        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => itemIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Name })
            .ToListAsync(cancellationToken);

        var culture = CultureCode.Create(tenantContext.RequireTenant().DefaultCulture).Value;
        var dishes = items
            .Select(item => (item, stats: perDish[item.Id.Value]))
            .Select(pair => new DishStatistics(pair.item.Id.Value, pair.item.Name.Resolve(culture, culture), pair.stats.Opens, pair.stats.ModelViews, pair.stats.ArStarts))
            .OrderByDescending(dish => dish.Opens)
            .ThenByDescending(dish => dish.ArStarts)
            .ThenBy(dish => dish.Name, StringComparer.CurrentCulture)
            .ToList();

        return new MenuStatisticsResponse(
            from,
            to,
            new MenuStatisticsTotals(Sum(MenuViewed), Sum(DishOpened), Sum(ModelViewed), Sum(ArStarted)),
            days,
            dishes);
    }
}
