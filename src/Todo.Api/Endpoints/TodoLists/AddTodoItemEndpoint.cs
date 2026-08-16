using Mediator;
using Todo.Api.Common;
using Todo.Application.TodoLists.Commands.AddTodoItem;
using Todo.Application.TodoLists.Dtos;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>POST /api/todo-lists/{todoListId}/items</c> - adds a TodoItem to a TodoList.
/// </summary>
/// <remarks>
/// The route says which TodoList, so the body carries only what is left - which is why the command
/// is assembled here from the two instead of being bound whole from the body. That is the endpoint's
/// entire contribution: bind, send, translate. A TodoList that does not exist comes back as a
/// not-found failure from the handler and is rendered by the one translation, so no status code is
/// decided here.
/// </remarks>
internal sealed class AddTodoItemEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/{todoListId:guid}/items", async (
                Guid todoListId,
                AddTodoItemRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new AddTodoItemCommand(todoListId, request.Description), cancellationToken)
                    .ConfigureAwait(false);

                // Location is the TodoList, not the TodoItem. A TodoItem has no address of its own
                // because it is not retrievable on its own - it is reached, and now created,
                // through its aggregate root, and it is on that representation that it appears.
                return result.ToCreated(_ => $"{GroupPrefix}/{todoListId}");
            })
            .WithName("AddTodoItem")
            .WithSummary("Adds a TodoItem to a TodoList.")
            .Produces<TodoItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// The body of a request to add a TodoItem.
/// </summary>
/// <param name="Description">What the new TodoItem should ask someone to do.</param>
internal sealed record AddTodoItemRequest(string Description);
