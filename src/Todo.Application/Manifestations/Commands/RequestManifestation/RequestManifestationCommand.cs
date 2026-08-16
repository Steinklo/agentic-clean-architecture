using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists;
using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Commands.RequestManifestation;

/// <summary>
/// Requests that a TodoItem be made true in the physical world.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList owning the TodoItem.</param>
/// <param name="TodoItemId">Identity of the TodoItem to manifest.</param>
public sealed record RequestManifestationCommand(Guid TodoListId, Guid TodoItemId)
    : IRequest<Result<ManifestationDto>>;

/// <summary>
/// Answers <see cref="RequestManifestationCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two aggregates, and only one of them changes.</b> The TodoList is loaded to answer one
/// question - does this TodoItem exist? - and is never mutated, so the single save writes one
/// aggregate: the new Manifestation. Existence across an aggregate boundary is the use case's to
/// check, because the Manifestation cannot see the TodoList and must not be given a way to.
/// </para>
/// <para>
/// The TodoList is not asked whether it is archived. Requesting a Manifestation changes nothing
/// about the TodoList, so there is no modification for an archived TodoList to refuse - and
/// inventing that rule here would be a domain invariant written outside the domain.
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

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(ManifestationDto.FromDomain(creation.Value));
    }

    /// <summary>
    /// This handler's own answer when there is no such TodoList, carrying the one code every route
    /// for that aggregate shares.
    /// </summary>
    private static DomainError TodoListNotFound(Guid todoListId) => DomainError.NotFound(
        "TodoList.NotFound",
        string.Create(CultureInfo.InvariantCulture, $"No TodoList with id '{todoListId}' exists."));

    /// <summary>
    /// This handler's own answer when the TodoList exists and does not own that TodoItem - the same
    /// code <c>TodoList.CompleteItem</c> raises, because a caller sees one
    /// <c>TodoList.Item.NotFound</c> whichever route produced it.
    /// </summary>
    private static DomainError TodoItemNotFound(Guid todoItemId) => DomainError.NotFound(
        "TodoList.Item.NotFound",
        string.Create(
            CultureInfo.InvariantCulture,
            $"No TodoItem with id '{todoItemId}' belongs to this TodoList."));
}
