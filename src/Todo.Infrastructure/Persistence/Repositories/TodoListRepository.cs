using Microsoft.EntityFrameworkCore;
using Todo.Application.TodoLists;
using Todo.Domain.TodoLists;

namespace Todo.Infrastructure.Persistence.Repositories;

/// <summary>
/// Satisfies <see cref="ITodoListRepository"/> against SQL Server through
/// <see cref="TodoDbContext"/>.
/// </summary>
/// <remarks>
/// <see cref="DbContext.Set{TEntity}()"/> rather than a <c>DbSet</c> property on the context: the
/// context stays free of per-aggregate members, so adding an aggregate means adding a
/// configuration and a repository and editing nothing that already exists.
/// <para>
/// Queries here are tracked. The aggregate is loaded so it can be changed and saved whole, which
/// is exactly what tracking is for; a read model that never changes anything would be a different
/// abstraction, not this one with <c>AsNoTracking</c> bolted on.
/// </para>
/// </remarks>
internal sealed class TodoListRepository(TodoDbContext context) : ITodoListRepository
{
    private readonly TodoDbContext _context = context;

    public void Add(TodoList todoList) => _context.Set<TodoList>().Add(todoList);

    public Task<TodoList?> GetByIdAsync(Guid todoListId, CancellationToken cancellationToken = default) =>
        _context
            .Set<TodoList>()

            // The aggregate is loaded whole. Its invariants span its TodoItems - archival is
            // refused while any is incomplete - so an aggregate loaded without them could enforce
            // that rule against a collection it merely believes is empty.
            .Include(todoList => todoList.Items)
            .FirstOrDefaultAsync(todoList => todoList.Id == todoListId, cancellationToken);
}
