using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Todo.Infrastructure;

namespace Todo.ArchitectureTests;

/// <summary>
/// The composition root, and the one thing in a slice that nothing discovers for you.
/// </summary>
/// <remarks>
/// Handlers, validators, entity configurations and endpoints all arrive by assembly scan, so a
/// feature adds no dependency-injection line for any of them. A repository is the exception: it is
/// registered by hand. Forget it and the build stays green, every test that does not resolve a
/// handler keeps passing, and the failure surfaces at the first real request as
/// <c>Unable to resolve service for type 'ITodoListRepository'</c>.
/// <para>
/// This asks the real container rather than reading <c>ConfigureServices.cs</c> as text, so it
/// cannot be fooled by a registration that is written and unreachable.
/// </para>
/// </remarks>
public sealed class CompositionTests
{
    /// <summary>Rule: <see cref="Rules.RepositoriesAreRegistered"/>.</summary>
    [Fact]
    public void Repositories_EveryInterface_IsRegisteredByInfrastructure()
    {
        var registered = InfrastructureRegistrations();

        Rule.OverTypes(
            Rules.RepositoriesAreRegistered,
            RepositoryInterfaces(),
            contract => registered.Contains(contract)
                ? null
                : $"is declared in Todo.Application and never registered. Add "
                  + $"services.AddScoped<{contract.Name}, {contract.Name[1..]}>(); to "
                  + "Todo.Infrastructure/ConfigureServices.cs. Nothing discovers this one: the "
                  + "build stays green and the first request throws.");
    }

    /// <summary>
    /// Every service type Infrastructure registers. The connection string is a placeholder —
    /// <c>AddDbContext</c> does not open a connection when it is registered, so no database is
    /// needed and this stays a container-free rule.
    /// </summary>
    private static HashSet<Type> InfrastructureRegistrations()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{ConfigureServices.ConnectionStringName}"] =
                        "Server=architecture-tests-never-connect;Database=None;",
                })
            .Build();

        return [.. new ServiceCollection()
            .AddInfrastructureServices(configuration)
            .Select(descriptor => descriptor.ServiceType)];
    }

    /// <summary>
    /// Repository contracts, identified by where they live and how they are named: an interface in
    /// Todo.Application called <c>I…Repository</c>.
    /// </summary>
    private static IReadOnlyCollection<Type> RepositoryInterfaces() =>
        [.. Layers.Application.AuthoredTypes
            .Where(type => type.IsInterface)
            .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal))];
}
