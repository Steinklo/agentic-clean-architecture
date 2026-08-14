using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.TodoLists.Dtos;
using Todo.Domain.Common;
using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists.Commands.AddTodoItem;

/// <summary>
/// Adds a TodoItem to a TodoList.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList to add to.</param>
/// <param name="Description">What the new TodoItem should ask someone to do.</param>
public sealed record AddTodoItemCommand(Guid TodoListId, string Description) : IRequest<Result<TodoItemDto>>;

/// <summary>
/// Answers <see cref="AddTodoItemCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// The aggregate is loaded whole, asked to change itself, and saved whole. There is no TodoItem
/// repository and nothing here inserts a TodoItem: <see cref="TodoList.AddItem"/> is the only way
/// one comes into existence, and the unit of work writes whatever the aggregate now holds.
/// </para>
/// <para>
/// The two failures below are different in kind and the handler treats them differently. A missing
/// TodoList is this handler's own answer - it asked for an aggregate and there was none. Whether a
/// description is acceptable, and whether an archived TodoList will accept anything at all, are the
/// aggregate's decisions, returned as a failed <see cref="Result"/> and passed straight out.
/// </para>
/// </remarks>
internal sealed class AddTodoItemHandler(ITodoListRepository todoLists, IUnitOfWork unitOfWork)
    : IRequestHandler<AddTodoItemCommand, Result<TodoItemDto>>
{
    private readonly ITodoListRepository _todoLists = todoLists;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result<TodoItemDto>> Handle(
        AddTodoItemCommand request,
        CancellationToken cancellationToken)
    {
        var todoList = await _todoLists
            .GetByIdAsync(request.TodoListId, cancellationToken)
            .ConfigureAwait(false);

        if (todoList is null)
        {
            return Result.Failure<TodoItemDto>(TodoListNotFound(request.TodoListId));
        }

        var addition = todoList.AddItem(request.Description);

        if (addition.IsFailure)
        {
            return Result.Failure<TodoItemDto>(addition.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(TodoItemDto.FromDomain(addition.Value));
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
