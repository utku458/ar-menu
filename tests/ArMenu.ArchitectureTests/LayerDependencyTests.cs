using System.Reflection;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Infrastructure;
using NetArchTest.Rules;

namespace ArMenu.ArchitectureTests;

/// <summary>
/// Clean Architecture's dependency rule, enforced by the build: source code dependencies only point inwards.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ITenantContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DependencyInjection).Assembly;

    private const string Application = "ArMenu.Application";
    private const string Infrastructure = "ArMenu.Infrastructure";
    private const string Api = "ArMenu.Api";
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";
    private const string Npgsql = "Npgsql";
    private const string IdentityModel = "Microsoft.IdentityModel";
    private const string Mediator = "Mediator";
    private const string FluentValidation = "FluentValidation";

    [Fact]
    public void Domain_depends_on_nothing_but_the_base_class_library()
    {
        AssertNoDependencies(
            DomainAssembly, Application, Infrastructure, Api, EntityFrameworkCore, AspNetCore, Npgsql, IdentityModel, Mediator, FluentValidation);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_frameworks()
    {
        AssertNoDependencies(ApplicationAssembly, Infrastructure, Api, EntityFrameworkCore, AspNetCore, Npgsql, IdentityModel);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_web_layer()
    {
        AssertNoDependencies(InfrastructureAssembly, Api, AspNetCore);
    }

    private static void AssertNoDependencies(Assembly assembly, params string[] forbiddenNamespaces)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"{assembly.GetName().Name} must not depend on [{string.Join(", ", forbiddenNamespaces)}]. Offending types: " +
            string.Join(", ", result.FailingTypes?.Select(type => type.FullName) ?? []));
    }
}
