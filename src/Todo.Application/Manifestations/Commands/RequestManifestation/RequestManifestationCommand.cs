using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.Manifestations.Abstractions;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Abstractions;
using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Commands.RequestManifestation;

/// <summary>
/// Records a request to make a TodoItem true in the physical world.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList the TodoItem belongs to.</param>
/// <param name="TodoItemId">Identity of the TodoItem to manifest.</param>
public sealed record RequestManifestationCommand(Guid TodoListId, Guid TodoItemId)
    : IRequest<Result<ManifestationDto>>;

/// <summary>
/// Answers <see cref="RequestManifestationCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two aggregates are involved and only one of them is written. The TodoList is loaded to answer a
/// question — does this TodoItem exist? — and is never changed, so this stays one aggregate per
/// transaction. Reading a second aggregate to validate a reference is not the same as writing two;
/// what would break the boundary is changing the TodoList here, and nothing does.
/// </para>
/// <para>
/// The reference is then held as a pair of ids on the Manifestation. It is deliberately not a
/// foreign key that the database would enforce: the two aggregates have separate lifecycles, and a
/// constraint spanning them would make one unable to change without the other's permission.
/// </para>
/// </remarks>
internal sealed class RequestManifestationHandler(
    ITodoListRepository todoLists,
    IManifestationRepository manifestations,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestManifestationCommand, Result<ManifestationDto>>
{
    private readonly ITodoListRepository _todoLists = todoLists;
    private readonly IManifestationRepository _manifestations = manifestations;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result<ManifestationDto>> Handle(
        RequestManifestationCommand request,
        CancellationToken cancellationToken)
    {
        var todoList = await _todoLists
            .GetByIdAsync(request.TodoListId, cancellationToken)
            .ConfigureAwait(false);

        if (todoList is null)
        {
            return Result.Failure<ManifestationDto>(TodoListNotFound(request.TodoListId));
        }

        if (!todoList.Items.Any(item => item.Id == request.TodoItemId))
        {
            return Result.Failure<ManifestationDto>(TodoItemNotFound(request.TodoItemId));
        }

        var creation = Manifestation.Create(request.TodoListId, request.TodoItemId);

        if (creation.IsFailure)
        {
            return Result.Failure<ManifestationDto>(creation.Error);
        }

        _manifestations.Add(creation.Value);

        // Saving is what dispatches ManifestationRequestedEvent, immediately before the write.
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(ManifestationDto.FromDomain(creation.Value));
    }

    /// <summary>
    /// This handler's own answer when there is no such TodoList, written beside the guard that
    /// raises it and carrying the code every TodoList route already shares.
    /// </summary>
    private static DomainError TodoListNotFound(Guid todoListId) => DomainError.NotFound(
        "TodoList.NotFound",
        string.Create(CultureInfo.InvariantCulture, $"No TodoList with id '{todoListId}' exists."));

    /// <summary>
    /// The same code <c>TodoList.CompleteItem</c> raises, so a caller sees one
    /// <c>TodoList.Item.NotFound</c> whichever route asked for a TodoItem that is not there.
    /// </summary>
    private static DomainError TodoItemNotFound(Guid todoItemId) => DomainError.NotFound(
        "TodoList.Item.NotFound",
        string.Create(
            CultureInfo.InvariantCulture,
            $"No TodoItem with id '{todoItemId}' belongs to this TodoList."));
}
