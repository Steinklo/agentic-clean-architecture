using System.Reflection;
using Todo.Api.Endpoints;
using Todo.Application;
using Todo.Infrastructure;
using Todo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Composition, outermost layer inwards. Each layer exposes exactly one entry point, and this is
// the only file that calls all of them - which is what keeps Entity Framework Core inside
// AddInfrastructureServices, where an architecture test can prove it stayed.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Applying migrations here is a development convenience and nothing more - see
// DatabaseMigrator for why it is the wrong shape for production. It is off unless
// Database:MigrateOnStartup says otherwise, which only docker-compose.yml and
// appsettings.Development.json do.
if (app.Configuration.GetValue<bool>(DatabaseMigrator.MigrateOnStartupKey))
{
    await app.Services.MigrateDatabaseAsync().ConfigureAwait(false);
}

// Reports real connectivity: it opens a connection to the database rather than merely
// confirming the process is running.
app.MapGet("/health", async (IDatabaseConnectivity database, CancellationToken cancellationToken) =>
    await database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
        ? Results.Ok(new HealthResponse("Healthy"))
        : Results.Json(
            new HealthResponse("Unhealthy"),
            statusCode: StatusCodes.Status503ServiceUnavailable));

app.MapEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// The body returned by the health endpoint.
/// </summary>
/// <param name="Status">Either <c>Healthy</c> or <c>Unhealthy</c>.</param>
internal sealed record HealthResponse(string Status);

/// <summary>
/// Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> in the integration tests can host this
/// application in process. Top-level statements otherwise generate an internal entry point class.
/// </summary>
public partial class Program;
