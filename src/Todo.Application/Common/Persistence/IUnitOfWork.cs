namespace Todo.Application.Common.Persistence;

/// <summary>
/// One atomic save of everything the current request changed.
/// </summary>
/// <remarks>
/// Handlers change aggregates through a repository and then commit once, here. The implementation
/// dispatches the domain events recorded by those aggregates immediately before the underlying
/// save, so anything a domain-event handler changes is written by the same save - and therefore
/// inside the same transaction - as the change that raised the event.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Dispatches recorded domain events, then persists every change made in this scope.
    /// </summary>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
