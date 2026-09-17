using FluentValidation;

namespace ArMenu.Application.Menus.Statistics.RecordMenuEvents;

public sealed class RecordMenuEventsCommandValidator : AbstractValidator<RecordMenuEventsCommand>
{
    /// <summary>A guest's batch is a handful of events; anything larger is not a guest.</summary>
    public const int MaxEvents = 20;

    public RecordMenuEventsCommandValidator()
    {
        RuleFor(command => command.Events).NotEmpty().Must(events => events.Count <= MaxEvents)
            .WithErrorCode("menu_events.too_many").WithMessage($"At most {MaxEvents} events can be sent at once.");

        RuleForEach(command => command.Events).ChildRules(menuEvent =>
        {
            menuEvent.RuleFor(input => input.Type).IsInEnum();
            menuEvent.RuleFor(input => input.ItemId)
                .Must((input, itemId) => input.Type.IsAboutADish() ? itemId is { } id && id != Guid.Empty : itemId is null)
                .WithErrorCode("menu_events.item_mismatch")
                .WithMessage("Dish events need the dish's id; menu events have none.");
        });
    }
}
