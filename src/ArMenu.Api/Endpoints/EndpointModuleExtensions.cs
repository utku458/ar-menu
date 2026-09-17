using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArMenu.Api.Endpoints;

internal static class EndpointModuleExtensions
{
    public static IServiceCollection AddEndpointModules(this IServiceCollection services, Assembly assembly)
    {
        var moduleTypes = assembly.DefinedTypes.Where(type =>
            type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpointModule)));

        foreach (var moduleType in moduleTypes)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointModule), moduleType));
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in endpoints.ServiceProvider.GetServices<IEndpointModule>())
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
