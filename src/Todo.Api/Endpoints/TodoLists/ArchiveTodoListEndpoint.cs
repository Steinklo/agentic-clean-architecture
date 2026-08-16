using Mediator;
using Todo.Api.Common;
using Todo.Application.TodoLists.Commands.ArchiveTodoList;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>POST /api/todo-lists/{todoListId}/archive</c> - retires a TodoList from active use.
/// </summary>
/// <remarks>
/// Archival is a named transition of the aggregate rather than an edit of an <c>IsArchived</c>
/// field, so it is its own sub-resource and takes no body: the caller asks for the transition and
/// the domain decides whether it is allowed. A TodoList still holding an incomplete TodoItem comes
/// back as a conflict-categorised failure carrying <c>TodoList.Archive.IncompleteItems</c>, and it
/// becomes a 409 in the one result translation - nothing about that is decided here.
/// </remarks>
internal sealed class ArchiveTodoListEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/{todoListId:guid}/archive", async (
                Guid todoListId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new ArchiveTodoListCommand(todoListId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToNoContent();
            })
            .WithName("ArchiveTodoList")
            .WithSummary("Retires a TodoList from active use.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
