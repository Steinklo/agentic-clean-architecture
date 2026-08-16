using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Todo.Infrastructure;

namespace Todo.IntegrationTests;

/// <summary>
/// Hosts the real <c>Todo.Api</c> in process, pointed at the containerised SQL Server.
/// </summary>
/// <remarks>
/// The connection string is overridden through configuration rather than by re-registering
/// services. That keeps the test host running the exact same composition as production - if
/// <c>AddInfrastructureServices</c> is wrong, these tests find out.
/// </remarks>
internal sealed class TodoApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Run as "Testing", not the default "Development". appsettings.Development.json carries a
        // connection string for running the API by hand against docker compose, and it would
        // otherwise win over the override below - pointing the whole suite at localhost:1433 and
        // failing with a connection timeout that looks nothing like a configuration problem.
        builder.UseEnvironment("Testing");

        // UseSetting, not ConfigureAppConfiguration. Under minimal hosting the WebApplicationBuilder
        // in Program.cs reads its configuration eagerly, before WebApplicationFactory's
        // ConfigureAppConfiguration callbacks run - so an in-memory source added there never reaches
        // AddInfrastructureServices. UseSetting writes into host configuration, which is chained
        // into the application's configuration and is therefore visible in time.
        builder.UseSetting(
            $"ConnectionStrings:{ConfigureServices.ConnectionStringName}",
            _connectionString);
    }
}
