using System.Globalization;
using Mediator;
using Todo.Application.TodoLists.Dtos;
using Todo.Domain.Common;

namespace Todo.Application.TodoLists.Queries.GetTodoList;

/// <summary>
/// Retrieves one TodoList.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList to retrieve.</param>
public sealed record GetTodoListQuery(Guid TodoListId) : IRequest<Result<TodoListDto>>;

/// <summary>
/// Answers <see cref="GetTodoListQuery"/>.
/// </summary>
/// <remarks>
/// A missing TodoList is an expected outcome, not an exception: it comes back as a failed
/// <see cref="Result"/> carrying a NotFound-categorised <see cref="DomainError"/>, which the API turns
/// into a 404 without the endpoint deciding anything.
/// </remarks>
internal sealed class GetTodoListHandler(ITodoListRepository todoLists)
    : IRequestHandler<GetTodoListQuery, Result<TodoListDto>>
{
    private readonly ITodoListRepository _todoLists = todoLists;

    public async ValueTask<Result<TodoListDto>> Handle(
        GetTodoListQuery request,
        CancellationToken cancellationToken)
    {
        var todoList = await _todoLists
            .GetByIdAsync(request.TodoListId, cancellationToken)
            .ConfigureAwait(false);

        return todoList is null
            ? Result.Failure<TodoListDto>(TodoListNotFound(request.TodoListId))
            : Result.Success(TodoListDto.FromDomain(todoList));
    }

    /// <summary>
    /// This handler's own answer when there is no such aggregate, written beside the guard that
    /// raises it. The code string is shared with the other TodoList use cases because callers -
    /// and <c>docs/api.md</c> - see one <c>TodoList.NotFound</c> whichever route produced it.
    /// </summary>
    private static DomainError TodoListNotFound(Guid todoListId) => DomainError.NotFound(
        "TodoList.NotFound",
        string.Create(CultureInfo.InvariantCulture, $"No TodoList with id '{todoListId}' exists."));
}
