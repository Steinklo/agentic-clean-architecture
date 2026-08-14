using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Todo.Infrastructure.Persistence;

/// <summary>
/// Lets the <c>dotnet ef</c> tools construct a <see cref="TodoDbContext"/> without booting the API.
/// </summary>
/// <remarks>
/// <para>
/// This is what keeps <c>Todo.Api</c> free of Entity Framework Core entirely - without it the tools
/// would demand a <c>Microsoft.EntityFrameworkCore.Design</c> package reference on the startup
/// project. Migrations are therefore added against Infrastructure alone:
/// </para>
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --project src/Todo.Infrastructure --startup-project src/Todo.Infrastructure --output-dir Persistence/Migrations
/// </code>
/// <para>
/// The connection string below is only ever used at design time. <c>migrations add</c> never opens
/// a connection; set <c>ConnectionStrings__TodoDb</c> in the environment before commands that do.
/// </para>
/// </remarks>
internal sealed class TodoDbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    /// <remarks>
    /// The same development-only literal as <c>docker-compose.yml</c> and
    /// <c>appsettings.Development.json</c>, so <c>dotnet ef</c> reaches the container that
    /// <c>docker compose up db</c> starts without anyone exporting anything first.
    /// </remarks>
    private const string DesignTimeConnectionString =
        "Server=localhost,1433;Database=Todo;User Id=sa;Password=LocalDevOnly_NotASecret1;Encrypt=True;TrustServerCertificate=True";

    public TodoDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable($"ConnectionStrings__{ConfigureServices.ConnectionStringName}")
            ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(TodoDbContext).Assembly.FullName))
            .Options;

        return new TodoDbContext(options);
    }
}
