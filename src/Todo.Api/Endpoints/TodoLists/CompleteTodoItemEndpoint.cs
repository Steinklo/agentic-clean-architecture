using Mediator;
using Todo.Api.Common;
using Todo.Application.TodoLists.Commands.CompleteTodoItem;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>POST /api/todo-lists/{todoListId}/items/{todoItemId}/complete</c> - marks a TodoItem complete.
/// </summary>
/// <remarks>
/// Completion is a named transition of the aggregate rather than an edit of a field, so it is its
/// own sub-resource: the caller asks for the transition and the domain decides whether it is
/// allowed. There is no body, so there is nothing for a caller to get half right.
/// </remarks>
internal sealed class CompleteTodoItemEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/{todoListId:guid}/items/{todoItemId:guid}/complete", async (
                Guid todoListId,
                Guid todoItemId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new CompleteTodoItemCommand(todoListId, todoItemId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToNoContent();
            })
            .WithName("CompleteTodoItem")
            .WithSummary("Marks a TodoItem complete.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
