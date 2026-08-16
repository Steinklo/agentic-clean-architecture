using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists;

/// <summary>
/// Loads and stores whole TodoList aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Declared here, in Application, and implemented in Infrastructure: the layer that needs
/// persistence owns the contract, and the layer that knows how to talk to SQL Server satisfies it.
/// That inversion is what keeps Entity Framework Core out of this project.
/// </para>
/// <para>
/// There is one repository per aggregate <em>root</em>, never one per entity. A TodoItem is
/// reached, added and completed through its TodoList, so it has no repository of its own.
/// </para>
/// <para>
/// Nothing here saves. Committing is <see cref="Common.Persistence.IUnitOfWork"/>'s job, so a
/// handler that changes two aggregates still writes them in one transaction.
/// </para>
/// </remarks>
public interface ITodoListRepository
{
    /// <summary>
    /// Records a new TodoList to be inserted by the next save.
    /// </summary>
    /// <param name="todoList">The TodoList to add.</param>
    void Add(TodoList todoList);

    /// <summary>
    /// Loads a whole TodoList by identity - its TodoItems included - tracked for change so callers
    /// can mutate it. There is no way to load part of one, because an aggregate that arrives
    /// partial cannot enforce the invariants that span the parts it did not bring.
    /// </summary>
    /// <param name="todoListId">The identity to load.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The TodoList, or <see langword="null"/> when no such TodoList exists.</returns>
    Task<TodoList?> GetByIdAsync(Guid todoListId, CancellationToken cancellationToken = default);
}
