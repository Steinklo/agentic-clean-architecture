using Mediator;
using Todo.Api.Common;
using Todo.Application.TodoLists.Dtos;
using Todo.Application.TodoLists.Queries.GetTodoList;

namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// <c>GET /api/todo-lists/{todoListId}</c> - retrieves one TodoList.
/// </summary>
internal sealed class GetTodoListEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapGet("/{todoListId:guid}", async (
                Guid todoListId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new GetTodoListQuery(todoListId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToOk();
            })
            .WithName("GetTodoList")
            .WithSummary("Retrieves a TodoList by its identity.")
            .Produces<TodoListDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
