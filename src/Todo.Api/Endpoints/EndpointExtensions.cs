using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Todo.Api.Endpoints;

/// <summary>
/// Discovers <see cref="IEndpoint"/> implementations and maps them, grouped per feature.
/// </summary>
internal static class EndpointExtensions
{
    /// <summary>
    /// Registers every <see cref="IEndpoint"/> in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] endpoints =
        [
            .. assembly
                .DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
                .Where(type => type.IsAssignableTo(typeof(IEndpoint)))
                .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
        ];

        services.TryAddEnumerable(endpoints);

        return services;
    }

    /// <summary>
    /// Maps every registered endpoint, creating one route group per feature prefix.
    /// </summary>
    /// <param name="app">The application to map onto.</param>
    /// <returns>The same <paramref name="app"/>, for chaining.</returns>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var features = app
            .Services
            .GetRequiredService<IEnumerable<IEndpoint>>()
            .GroupBy(endpoint => endpoint.GroupPrefix, StringComparer.Ordinal);

        foreach (var feature in features)
        {
            // One RouteGroupBuilder per feature, so anything that should apply to a whole feature -
            // a tag, later an authorisation policy or a rate limit - is stated once, on the group.
            var group = app.MapGroup(feature.Key).WithTags(feature.First().GroupTag);

            foreach (var endpoint in feature)
            {
                endpoint.MapEndpoint(group);
            }
        }

        return app;
    }
}
