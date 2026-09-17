namespace ArMenu.Api.Endpoints;

/// <summary>
/// A cohesive group of endpoints, typically one feature. Modules are discovered from the assembly and mapped at
/// startup, which keeps <c>Program.cs</c> free of feature details and lets tests plug in extra endpoints.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
