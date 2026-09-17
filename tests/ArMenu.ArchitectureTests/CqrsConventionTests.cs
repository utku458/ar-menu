using System.Reflection;
using ArMenu.Api.Endpoints;
using ArMenu.Application.MultiTenancy;
using ArMenu.Infrastructure;
using FluentValidation;
using Mediator;
using NetArchTest.Rules;

namespace ArMenu.ArchitectureTests;

/// <summary>
/// The CQRS split, enforced: writes go through aggregates in the application layer, reads are projections next to the
/// database, and HTTP endpoints only ever talk to the mediator.
/// </summary>
public sealed class CqrsConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ITenantContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(IEndpointModule).Assembly;

    [Fact]
    public void Command_handlers_live_in_the_application_layer()
    {
        ImplementationsOf(ApplicationAssembly, typeof(ICommandHandler<,>)).ShouldNotBeEmpty();
        ImplementationsOf(InfrastructureAssembly, typeof(ICommandHandler<,>)).ShouldBeEmpty();
    }

    [Fact]
    public void Query_handlers_live_next_to_the_database()
    {
        ImplementationsOf(InfrastructureAssembly, typeof(IQueryHandler<,>)).ShouldNotBeEmpty();
        ImplementationsOf(ApplicationAssembly, typeof(IQueryHandler<,>)).ShouldBeEmpty();
    }

    [Fact]
    public void Handlers_and_validators_are_sealed()
    {
        var unsealed = ImplementationsOf(ApplicationAssembly, typeof(ICommandHandler<,>))
            .Concat(ImplementationsOf(InfrastructureAssembly, typeof(IQueryHandler<,>)))
            .Concat(ImplementationsOf(ApplicationAssembly, typeof(AbstractValidator<>)))
            .Where(type => !type.IsSealed)
            .Select(type => type.Name);

        unsealed.ShouldBeEmpty();
    }

    [Fact]
    public void Commands_and_queries_are_immutable_records()
    {
        var messages = ImplementationsOf(ApplicationAssembly, typeof(ICommand<>))
            .Concat(ImplementationsOf(ApplicationAssembly, typeof(IQuery<>)))
            .ToList();

        messages.ShouldNotBeEmpty();
        messages.Where(type => !IsSealedRecord(type)).Select(type => type.Name).ShouldBeEmpty();
    }

    [Fact]
    public void Endpoints_reach_the_application_only_through_the_mediator()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("ArMenu.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny("ArMenu.Infrastructure", "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Endpoints must not touch persistence directly: " +
            string.Join(", ", result.FailingTypes?.Select(type => type.FullName) ?? []));
    }

    private static List<Type> ImplementationsOf(Assembly assembly, Type openGenericType) =>
        [.. assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && Implements(type, openGenericType))];

    private static bool Implements(Type type, Type openGenericType) =>
        openGenericType.IsInterface
            ? type.GetInterfaces().Any(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == openGenericType)
            : BaseTypes(type).Any(baseType => baseType.IsGenericType && baseType.GetGenericTypeDefinition() == openGenericType);

    private static IEnumerable<Type> BaseTypes(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    // The compiler emits a "<Clone>$" method for every record type.
    private static bool IsSealedRecord(Type type) =>
        type.IsSealed && type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
}
