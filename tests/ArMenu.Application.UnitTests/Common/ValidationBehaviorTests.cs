using ArMenu.Application.Common.Behaviors;
using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;
using FluentValidation;
using Mediator;

namespace ArMenu.Application.UnitTests.Common;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Valid_messages_reach_the_handler()
    {
        var behavior = new ValidationBehavior<RegisterDishCommand, Result<int>>([new RegisterDishCommandValidator()]);
        var handlerCalled = false;

        var result = await behavior.Handle(
            new RegisterDishCommand("Lahmacun", 120),
            (_, _) =>
            {
                handlerCalled = true;
                return ValueTask.FromResult<Result<int>>(42);
            },
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Invalid_messages_short_circuit_with_every_field_error()
    {
        var behavior = new ValidationBehavior<RegisterDishCommand, Result<int>>([new RegisterDishCommandValidator()]);
        var handlerCalled = false;

        var result = await behavior.Handle(
            new RegisterDishCommand(Name: "", Price: -1),
            (_, _) =>
            {
                handlerCalled = true;
                return ValueTask.FromResult<Result<int>>(42);
            },
            TestContext.Current.CancellationToken);

        handlerCalled.ShouldBeFalse();
        var error = result.Error.ShouldBeOfType<ValidationError>();
        error.Type.ShouldBe(ErrorType.Validation);
        error.Errors.Select(fieldError => (fieldError.Field, fieldError.Code)).ShouldBe(
        [
            ("Name", "validation.not_empty"),
            ("Price", "dish.price_negative"),
        ]);
    }

    [Fact]
    public async Task Messages_without_validators_pass_through()
    {
        var behavior = new ValidationBehavior<RegisterDishCommand, Result<int>>([]);

        var result = await behavior.Handle(
            new RegisterDishCommand("", -1),
            (_, _) => ValueTask.FromResult<Result<int>>(7),
            TestContext.Current.CancellationToken);

        result.Value.ShouldBe(7);
    }

    internal sealed record RegisterDishCommand(string Name, decimal Price) : ICommand<Result<int>>;

    private sealed class RegisterDishCommandValidator : AbstractValidator<RegisterDishCommand>
    {
        public RegisterDishCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
            RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithErrorCode("dish.price_negative");
        }
    }
}
