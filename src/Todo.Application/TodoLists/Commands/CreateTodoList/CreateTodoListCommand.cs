using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.TodoLists.Abstractions;
using Todo.Application.TodoLists.Dtos;
using Todo.Domain.Common;
using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists.Commands.CreateTodoList;

/// <summary>
/// Creates a TodoList.
/// </summary>
/// <remarks>
/// The command and the code that answers it live in one file, named for the command. Finding the
/// implementation of a use case is then a file-name lookup rather than a search - which matters
/// more for a template an agent extends than for one a person reads once.
/// </remarks>
/// <param name="Title">The title the new TodoList should have.</param>
public sealed record CreateTodoListCommand(string Title) : IRequest<Result<TodoListDto>>;

/// <summary>
/// Answers <see cref="CreateTodoListCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Named <c>...Handler</c>, not <c>...CommandHandler</c>: the command's name already says it is a
/// command, and an architecture test asserts the <c>Handler</c> suffix.
/// </para>
/// <para>
/// Note how little is here. Whether a title is acceptable is <see cref="TodoList.Create"/>'s
/// decision, and whether the request was even well formed was settled by the validation behaviour
/// before this ran. The handler's own job is only orchestration: ask the domain, store, commit,
/// project.
/// </para>
/// </remarks>
internal sealed class CreateTodoListHandler(ITodoListRepository todoLists, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTodoListCommand, Result<TodoListDto>>
{
    private readonly ITodoListRepository _todoLists = todoLists;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result<TodoListDto>> Handle(
        CreateTodoListCommand request,
        CancellationToken cancellationToken)
    {
        var creation = TodoList.Create(request.Title);

        if (creation.IsFailure)
        {
            return Result.Failure<TodoListDto>(creation.Error);
        }

        _todoLists.Add(creation.Value);

        // Saving is what dispatches TodoListCreatedEvent, immediately before the write, so a
        // domain-event handler's own changes are part of this same transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(TodoListDto.FromDomain(creation.Value));
    }
}
