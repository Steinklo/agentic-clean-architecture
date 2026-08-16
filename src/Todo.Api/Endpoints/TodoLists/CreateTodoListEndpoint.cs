using Mediator;
using Todo.Api.Common;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>POST /api/todo-lists</c> - creates a TodoList.
/// </summary>
/// <remarks>
/// The whole endpoint is: bind, send, translate. It names no status code, checks nothing about the
/// request, and knows nothing about how the TodoList is stored. Everything it would otherwise do
/// is somewhere it can only be written once - shape rules in the validator, invariants in the
/// aggregate, the status ladder in <see cref="ResultExtensions"/>.
/// </remarks>
internal sealed class CreateTodoListEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/", async (
                CreateTodoListCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);

                return result.ToCreated(todoList => $"{GroupPrefix}/{todoList.Id}");
            })
            .WithName("CreateTodoList")
            .WithSummary("Creates a TodoList.")
            .Produces<TodoListDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
