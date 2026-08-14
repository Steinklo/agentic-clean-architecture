using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Todo.Infrastructure.Persistence;

/// <summary>
/// Applies any pending migrations to the configured database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Migrating from application startup is a template convenience, not a production
/// pattern.</b> It exists so that a fresh <c>docker compose up</c> yields a usable database with no
/// manual step - which is the whole point of the compose file - and for no other reason. In
/// production it is wrong for reasons that only show up once it is too late to change: every
/// replica races to take the same migration lock during a rolling deploy, the application needs
/// schema-owner rights it should never hold at runtime, and a failed migration takes the process
/// down rather than failing a deployment step you can inspect and roll back. Apply migrations from
/// a deployment step instead - <c>dotnet ef migrations bundle</c> produces an executable built for
/// exactly that.
/// </para>
/// <para>
/// It is therefore <b>off unless something switches it on</b>. Nothing in
/// <c>appsettings.json</c> does; <c>docker-compose.yml</c> and
/// <c>appsettings.Development.json</c> both do, and they are the two development paths.
/// </para>
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>
    /// The configuration key that turns startup migration on.
    /// </summary>
    /// <remarks>
    /// As an environment variable that is <c>Database__MigrateOnStartup</c> - the double underscore
    /// is how the environment variable provider spells a section separator.
    /// </remarks>
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>
    /// How long to keep retrying before giving up and letting the host fail to start.
    /// </summary>
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How long to wait between attempts.
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Applies every migration that has not yet been applied.
    /// </summary>
    /// <param name="services">The root service provider; a scope is created internally.</param>
    /// <param name="cancellationToken">Cancels the wait between attempts.</param>
    /// <returns>A task that completes once the schema is up to date.</returns>
    /// <remarks>
    /// Migrations, never <c>EnsureCreated</c>: a migration that does not apply has to fail loudly,
    /// and <c>EnsureCreated</c> would quietly paper over exactly that.
    /// <para>
    /// The retry loop is not belt and braces. <c>docker-compose.yml</c> already waits for the
    /// database's healthcheck to pass before this process starts, but a container that has just
    /// finished recovery can still refuse a connection for a second or two, and the same is true of
    /// a database an IDE-run API reaches before the container is warm. Failing startup over that
    /// would be a confusing first experience of the template.
    /// </para>
    /// </remarks>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var scope = services.CreateAsyncScope();

        await using (scope.ConfigureAwait(false))
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            var deadline = DateTimeOffset.UtcNow + ReadinessTimeout;

            while (true)
            {
                try
                {
                    await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (SqlException) when (DateTimeOffset.UtcNow + RetryDelay < deadline)
                {
                    await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
