namespace Todo.Infrastructure.Persistence;

/// <summary>
/// Answers <see cref="IDatabaseConnectivity"/> by opening a real connection through
/// <see cref="TodoDbContext"/>.
/// </summary>
internal sealed class DatabaseConnectivity(TodoDbContext context) : IDatabaseConnectivity
{
    private readonly TodoDbContext _context = context;

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        _context.Database.CanConnectAsync(cancellationToken);
}
