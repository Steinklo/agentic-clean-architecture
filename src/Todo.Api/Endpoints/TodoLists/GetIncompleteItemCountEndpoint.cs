using Microsoft.EntityFrameworkCore;
using Todo.Domain.TodoLists.Entities;
using Todo.Infrastructure.Persistence;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>GET /api/todo-lists/{todoListId}/incomplete-count</c> - counts how many items on a TodoList
/// are still incomplete.
/// </summary>
internal sealed class GetIncompleteItemCountEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapGet("/{todoListId:guid}/incomplete-count", async (
                Guid todoListId,
                TodoDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var count = await dbContext.Set<TodoItem>()
                    .Where(item => item.TodoListId == todoListId && !item.IsComplete)
                    .CountAsync(cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(count);
            })
            .WithName("GetIncompleteItemCount")
            .WithSummary("Counts how many items on a TodoList are still incomplete.")
            .Produces<int>();
    }
}
