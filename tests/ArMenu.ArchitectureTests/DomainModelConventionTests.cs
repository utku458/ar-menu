using System.Reflection;
using ArMenu.Domain.Common;

namespace ArMenu.ArchitectureTests;

/// <summary>Conventions that keep the domain model encapsulated as it grows.</summary>
public sealed class DomainModelConventionTests
{
    private static readonly Type[] DomainTypes = typeof(Entity<>).Assembly.GetTypes();

    [Fact]
    public void Domain_state_can_only_change_through_behavior_methods()
    {
        var publiclySettableProperties = DomainTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod is { IsPublic: true } && !IsInitOnly(property))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        publiclySettableProperties.ShouldBeEmpty();
    }

    [Fact]
    public void Aggregates_are_sealed()
    {
        var unsealedAggregates = DomainTypes
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IAggregateRoot).IsAssignableFrom(type) && !type.IsSealed)
            .Select(type => type.Name)
            .ToList();

        unsealedAggregates.ShouldBeEmpty();
    }

    [Fact]
    public void Aggregates_expose_no_public_constructors()
    {
        // Creation must go through factory methods that validate invariants and return a Result.
        var aggregatesWithPublicConstructors = DomainTypes
            .Where(type => typeof(IAggregateRoot).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(type => type.Name)
            .ToList();

        aggregatesWithPublicConstructors.ShouldBeEmpty();
    }

    [Fact]
    public void Tenant_scoped_types_are_aggregate_roots()
    {
        // Isolation is enforced per aggregate; a tenant-scoped child entity would need its own guarantees.
        var tenantScopedNonAggregates = DomainTypes
            .Where(type => typeof(ITenantScoped).IsAssignableFrom(type) && type.IsClass && !typeof(IAggregateRoot).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        tenantScopedNonAggregates.ShouldBeEmpty();
    }

    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
}
