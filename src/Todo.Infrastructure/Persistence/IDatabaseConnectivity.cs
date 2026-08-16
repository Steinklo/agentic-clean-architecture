namespace Todo.Infrastructure.Persistence;

/// <summary>
/// Reports whether the application can actually reach its database.
/// </summary>
/// <remarks>
/// This exists so the API can expose a health endpoint that means something without naming an
/// Entity Framework Core type. Every EF Core reference stays behind this abstraction.
/// </remarks>
public interface IDatabaseConnectivity
{
    /// <summary>
    /// Opens a connection to the database and reports whether it succeeded.
    /// </summary>
    /// <param name="cancellationToken">Cancels the connection attempt.</param>
    /// <returns><see langword="true"/> when the database answered; otherwise <see langword="false"/>.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
