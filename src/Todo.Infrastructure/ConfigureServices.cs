using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Todo.Application.Common.Persistence;
using Todo.Application.TodoLists.Abstractions;
using Todo.Infrastructure.Persistence;
using Todo.Infrastructure.Persistence.Repositories;

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

        // AddDbContext registers TodoDbContext, not the base DbContext it derives from. This exists
        // for exactly one caller today: CountIncompleteItemsHandler, which asks for the base type
        // precisely because Application cannot see TodoDbContext without a project reference it is
        // not allowed to have. Without this, that handler compiles, passes every rule but the ORM
        // one it exists to break, and throws at resolution time for a reason nobody intended.
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TodoDbContext>());

        services.AddScoped<IDatabaseConnectivity, DatabaseConnectivity>();

        // One repository per aggregate root, plus the single unit of work they all commit through.
        // Both contracts are declared in Application; only these lines know an EF Core type exists.
        services.AddScoped<ITodoListRepository, TodoListRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
