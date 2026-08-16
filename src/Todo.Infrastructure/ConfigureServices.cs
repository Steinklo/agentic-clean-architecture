using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Todo.Application.Common.Persistence;
using Todo.Application.Manifestations;
using Todo.Application.TodoLists;
using Todo.Infrastructure.Persistence;
using Todo.Infrastructure.Persistence.Repositories;
using Todo.Infrastructure.Reality;

namespace Todo.Infrastructure;

/// <summary>
/// Composition root for everything Infrastructure owns.
/// </summary>
/// <remarks>
/// Every Entity Framework Core registration happens in here. <c>Todo.Api</c> must never name an
/// EF Core type directly - an architecture test enforces that - so the API's only entry point into
/// persistence is <see cref="AddInfrastructureServices"/>.
/// </remarks>
public static class ConfigureServices
{
    /// <summary>
    /// The name of the connection string this application reads from configuration.
    /// </summary>
    public const string ConnectionStringName = "TodoDb";

    /// <summary>
    /// Registers the persistence stack against SQL Server.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration supplying the <c>TodoDb</c> connection string.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The <c>TodoDb</c> connection string is missing or empty.
    /// </exception>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' was not found. Set " +
                $"ConnectionStrings:{ConnectionStringName} in configuration, or the " +
                $"ConnectionStrings__{ConnectionStringName} environment variable.");
        }

        services.AddDbContext<TodoDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(TodoDbContext).Assembly.FullName)));

        services.AddScoped<IDatabaseConnectivity, DatabaseConnectivity>();

        // One repository per aggregate root, plus the single unit of work they all commit through.
        // Both contracts are declared in Application; only these lines know an EF Core type exists.
        services.AddScoped<ITodoListRepository, TodoListRepository>();
        services.AddScoped<IManifestationRepository, ManifestationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Not a repository, and registered here for the same reason: a port declared in Application
        // whose only implementation knows about the world outside. This one knows there is nothing
        // out there and says so - see RealityGateway.
        services.AddScoped<IRealityGateway, RealityGateway>();

        return services;
    }
}
