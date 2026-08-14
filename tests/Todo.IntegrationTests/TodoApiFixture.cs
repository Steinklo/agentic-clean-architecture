using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;
using Todo.Infrastructure.Persistence;

namespace Todo.IntegrationTests;

/// <summary>
/// Owns the one SQL Server container and the one API host that the whole integration suite shares.
/// </summary>
/// <remarks>
/// Wired up as an xUnit collection fixture (see <see cref="TodoApiCollection"/>), so it is created
/// once for the entire collection rather than once per test class or test.
/// </remarks>
public sealed class TodoApiFixture : IAsyncLifetime
{
    /// <summary>
    /// The SQL Server image, pinned to an exact cumulative update.
    /// </summary>
    /// <remarks>
    /// Pinned deliberately: <c>latest</c> would let a server-side retag change what the suite runs
    /// against without a commit, and Testcontainers no longer supplies an implicit default.
    /// </remarks>
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04";

    /// <summary>
    /// The database migrations are applied to. Not <c>master</c>, which Respawn must never reset.
    /// </summary>
    private const string DatabaseName = "TodoIntegrationTests";

    /// <summary>
    /// Generous enough to cover a cold image pull, short enough that an unsupported host fails with
    /// an explanation instead of hanging.
    /// </summary>
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long to keep trying to open a connection before declaring the server unusable.
    /// </summary>
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromMinutes(3);

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    private TodoApiFactory? _factory;
    private Respawner? _respawner;
    private string? _connectionString;

    /// <summary>
    /// Records what the host logs while a test asks it to. Off unless a test opens a recording.
    /// </summary>
    /// <remarks>
    /// Registered on the host's own <see cref="ILoggerFactory"/> rather than through configuration,
    /// so the composition under test stays byte-for-byte the production one - the same reason the
    /// connection string is overridden and no service is re-registered.
    /// </remarks>
    public LogCapture Logs { get; } = new();

    /// <summary>
    /// The connection string for the migrated test database inside the container.
    /// </summary>
    public string ConnectionString =>
        _connectionString ?? throw new InvalidOperationException("The fixture has not been initialised.");

    /// <summary>
    /// Creates a client that speaks real HTTP to the in-process API.
    /// </summary>
    /// <returns>A new <see cref="HttpClient"/>; the caller disposes it.</returns>
    public HttpClient CreateClient() =>
        (_factory ?? throw new InvalidOperationException("The fixture has not been initialised.")).CreateClient();

    /// <summary>
    /// Deletes every row from every mapped table, leaving the schema and migration history intact.
    /// </summary>
    /// <returns>A task that completes once the database is empty.</returns>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Built on first use rather than during initialisation, because Respawn refuses to build
        // against a schema with no tables - and until the first entity is mapped, that is exactly
        // what the schema is. Doing it lazily means an empty schema is simply nothing to reset, and
        // resetting starts working on its own the moment a table exists. No flag to remember to flip.
        _respawner ??= await TryCreateRespawnerAsync(connection);

        if (_respawner is null)
        {
            return;
        }

        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Builds a respawner, or returns <see langword="null"/> when there is nothing to reset yet.
    /// </summary>
    private static async Task<Respawner?> TryCreateRespawnerAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME <> '__EFMigrationsHistory';
            """;

        var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

        if (tableCount == 0)
        {
            return null;
        }

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await StartContainerAsync();
        await WaitForSqlServerAsync(_container.GetConnectionString());

        _connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = DatabaseName,

            // The default 15 seconds leaves no headroom on a container that has only just finished
            // recovery, and a single slow connection here fails the entire suite.
            ConnectTimeout = 60,
        }.ConnectionString;

        _factory = new TodoApiFactory(_connectionString);

        _factory.Services.GetRequiredService<ILoggerFactory>().AddProvider(Logs);

        // Schema comes from migrations, never EnsureCreated: a migration that does not apply has to
        // fail the build, and EnsureCreated would quietly paper over exactly that.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    DescribeMigrationFailure(context.Database.GetConnectionString()),
                    exception);
            }
        }

    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private async Task StartContainerAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(StartupTimeout);
            await _container.StartAsync(timeout.Token);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(DescribeStartupFailure(), exception);
        }
    }

    /// <summary>
    /// Names what the migration was actually connecting to, with the password removed.
    /// </summary>
    /// <remarks>
    /// A misconfigured connection string and an unreachable server fail identically, with a
    /// connection timeout. A failure that does not name its target sends you to investigate Docker
    /// when the real problem is a setting being overridden somewhere you did not expect.
    /// </remarks>
    private static string DescribeMigrationFailure(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Applying migrations failed, and no connection string was configured.";
        }

        var redacted = new SqlConnectionStringBuilder(connectionString)
        {
            Password = "***",
        };

        return $"Applying migrations failed against server '{redacted.DataSource}', database "
            + $"'{redacted.InitialCatalog}'. If that is not the test container, something is "
            + $"overriding the test configuration. Connection string: {redacted.ConnectionString}";
    }

    /// <summary>
    /// Waits until the server actually accepts a connection from the host.
    /// </summary>
    /// <remarks>
    /// A started container is not a ready database. SQL Server still has to recover its system
    /// databases, which takes tens of seconds cold, and the container's own wait strategy has been
    /// observed reporting ready within about two seconds - long before the engine is listening.
    /// Without this, the first real query burns its connect timeout and the suite fails on a
    /// perfectly healthy machine.
    ///
    /// Polling a real connection is deliberate: it verifies the exact thing the tests need, and it
    /// does not depend on the path of a command-line tool inside the image, which has moved between
    /// SQL Server releases.
    /// </remarks>
    private static async Task WaitForSqlServerAsync(string connectionString)
    {
        var probe = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 5,
        }.ConnectionString;

        var deadline = DateTimeOffset.UtcNow + ReadinessTimeout;
        Exception? lastFailure = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await using var connection = new SqlConnection(probe);
                await connection.OpenAsync();
                return;
            }
            catch (SqlException exception)
            {
                lastFailure = exception;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperationException(DescribeStartupFailure(), lastFailure);
    }

    /// <summary>
    /// Turns the two ways container startup realistically fails into something actionable.
    /// </summary>
    /// <remarks>
    /// On Arm hardware the real cause is that no supported image exists, but Testcontainers surfaces
    /// it as an opaque wait-strategy timeout. Naming the limitation is the whole point of this
    /// method.
    /// </remarks>
    private static string DescribeStartupFailure()
    {
        var common =
            $"The SQL Server test container did not start (image: {SqlServerImage}).";

        return RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.Armv6 or Architecture.Arm
            ? $"""
               {common}

               This host's CPU architecture is {RuntimeInformation.OSArchitecture}, and that is
               almost certainly the reason. Microsoft ships SQL Server container images for x86-64
               only, and explicitly does not support running them under Rosetta 2, Prism or QEMU -
               that holds for every version, including SQL Server 2025. There is no Arm image to
               fall back to.

               Run the integration tests on an x86-64 host or in CI. Everything else in the
               solution, including the unit and architecture tests, runs fine here.
               """
            : $"""
               {common}

               Check that Docker is running and that the image can be pulled.

               Note also that Microsoft ships SQL Server container images for x86-64 only, with no
               support for Rosetta 2, Prism or QEMU at any version. If this is an Arm host reporting
               an x86-64 architecture through emulation, the container cannot work here.
               """;
    }
}
