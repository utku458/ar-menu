using FluentValidation;

namespace ArMenu.Application.History.Queries.GetHistory;

public sealed class GetHistoryQueryValidator : AbstractValidator<GetHistoryQuery>
{
    public GetHistoryQueryValidator() => RuleFor(query => query.Limit).InclusiveBetween(1, GetHistoryQuery.MaxLimit);
}
