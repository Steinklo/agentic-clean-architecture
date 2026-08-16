using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.TodoLists.Abstractions;
using Todo.Domain.Common;
using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists.Commands.ArchiveTodoList;

/// <summary>
/// Retires a TodoList from active use.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList to archive.</param>
public sealed record ArchiveTodoListCommand(Guid TodoListId) : IRequest<Result>;

/// <summary>
/// Answers <see cref="ArchiveTodoListCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the use case the aggregate boundary exists for. Archival is refused while any TodoItem is
/// incomplete, and that rule can only be decided by something holding every TodoItem at once - which
/// is why <c>GetByIdAsync</c> loads the TodoList with its Items and why nothing here inspects them.
/// The handler asks the aggregate and passes its answer out; a query that fetched the TodoList row
/// alone would make <see cref="TodoList.Archive"/> silently agree to anything.
/// </para>
/// <para>
/// Saving is also what dispatches <c>TodoListArchivedEvent</c>, immediately before the write, so a
/// handler for it commits inside the same transaction as the archival itself.
/// </para>
/// </remarks>
internal sealed class ArchiveTodoListHandler(ITodoListRepository todoLists, IUnitOfWork unitOfWork)
    : IRequestHandler<ArchiveTodoListCommand, Result>
{
    private readonly ITodoListRepository _todoLists = todoLists;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result> Handle(
        ArchiveTodoListCommand request,
        CancellationToken cancellationToken)
    {
        var todoList = await _todoLists
            .GetByIdAsync(request.TodoListId, cancellationToken)
            .ConfigureAwait(false);

        if (todoList is null)
        {
            return Result.Failure(TodoListNotFound(request.TodoListId));
        }

        var archival = todoList.Archive();

        if (archival.IsFailure)
        {
            return archival;
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
