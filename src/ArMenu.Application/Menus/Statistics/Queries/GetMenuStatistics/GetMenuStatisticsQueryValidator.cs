using FluentValidation;

namespace ArMenu.Application.Menus.Statistics.Queries.GetMenuStatistics;

public sealed class GetMenuStatisticsQueryValidator : AbstractValidator<GetMenuStatisticsQuery>
{
    public const int MaxDays = 90;

    public GetMenuStatisticsQueryValidator() => RuleFor(query => query.Days).InclusiveBetween(1, MaxDays);
}
