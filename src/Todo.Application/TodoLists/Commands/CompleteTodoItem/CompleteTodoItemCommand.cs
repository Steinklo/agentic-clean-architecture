using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.TodoLists.Abstractions;
using Todo.Domain.Common;
using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists.Commands.CompleteTodoItem;

/// <summary>
/// Marks a TodoItem complete.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList the item belongs to.</param>
/// <param name="TodoItemId">Identity of the TodoItem to complete.</param>
public sealed record CompleteTodoItemCommand(Guid TodoListId, Guid TodoItemId) : IRequest<Result>;

/// <summary>
/// Answers <see cref="CompleteTodoItemCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Completing an item is a change to the TodoList, not to a TodoItem reached on its own: the
/// aggregate is loaded whole, <see cref="TodoList.CompleteItem"/> finds the item and refuses if it
/// belongs to someone else, and the whole aggregate is saved. Loading the TodoItem directly would
/// route around the aggregate, and with it the rule that an archived TodoList accepts no changes.
/// </para>
/// <para>
/// Saving is also what dispatches <c>TodoItemCompletedEvent</c>, immediately before the write, so a
/// handler for it commits inside the same transaction.
/// </para>
/// </remarks>
internal sealed class CompleteTodoItemHandler(ITodoListRepository todoLists, IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteTodoItemCommand, Result>
{
    private readonly ITodoListRepository _todoLists = todoLists;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result> Handle(
        CompleteTodoItemCommand request,
        CancellationToken cancellationToken)
    {
        var todoList = await _todoLists
            .GetByIdAsync(request.TodoListId, cancellationToken)
            .ConfigureAwait(false);

        if (todoList is null)
        {
            return Result.Failure(TodoListNotFound(request.TodoListId));
        }

        var completion = todoList.CompleteItem(request.TodoItemId);

        if (completion.IsFailure)
        {
            return completion;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
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
