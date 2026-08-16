using System.Globalization;
using Todo.Domain.Common;
using Todo.Domain.TodoLists.Entities;
using Todo.Domain.TodoLists.Events;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Domain.TodoLists;

/// <summary>
/// The aggregate root: a named collection of TodoItems, responsible for enforcing every invariant
/// that spans more than one item. The invariant that justifies the boundary is archival — a TodoList
/// cannot be archived while any of its TodoItems is incomplete.
/// </summary>
public sealed class TodoList : AggregateRoot<Guid>
{
    private readonly List<TodoItem> _items = [];

    // Not mapped and not state: a cached view over _items, built on first read. EF writes the
    // backing field directly and never touches this, and a view stays live as _items changes.
    private IReadOnlyCollection<TodoItem>? _itemsView;

    private TodoList(Guid id, TodoListTitle title)
        : base(id) => Title = title;

    /// <summary>The name this TodoList is known by.</summary>
    public TodoListTitle Title { get; private set; }

    /// <summary>Whether the TodoList has been retired from active use.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>When the TodoList was archived, if it has been.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>
    /// The TodoItems in this TodoList. Read-only: items are added and completed through the
    /// aggregate root, never on the collection. EF Core writes the backing field directly.
    /// </summary>
    /// <remarks>
    /// <c>AsReadOnly()</c> allocates a fresh wrapper on every call, and this is read on every
    /// projection of the aggregate. The wrapper is a view over <c>_items</c>, not a copy, so one
    /// created once still reflects every later add — there is nothing to invalidate.
    /// </remarks>
    public IReadOnlyCollection<TodoItem> Items => _itemsView ??= _items.AsReadOnly();

    /// <summary>
    /// Creates a TodoList, returning a validation failure rather than throwing on invalid input.
    /// </summary>
    public static Result<TodoList> Create(string title)
    {
        var titleResult = TodoListTitle.Create(title);

        if (titleResult.IsFailure)
        {
            return Result.Failure<TodoList>(titleResult.Error);
        }

        var todoList = new TodoList(Guid.CreateVersion7(), titleResult.Value);
        todoList.RaiseDomainEvent(new TodoListCreatedEvent(todoList.Id, todoList.Title.Value));

        return Result.Success(todoList);
    }

    /// <summary>
    /// Adds a TodoItem to this TodoList. The only way a TodoItem comes into existence.
    /// </summary>
    public Result<TodoItem> AddItem(string description)
    {
        if (ArchivedRejection() is { } archived)
        {
            return Result.Failure<TodoItem>(archived);
        }

        var itemResult = TodoItem.Create(Id, description);

        if (itemResult.IsFailure)
        {
            return itemResult;
        }

        _items.Add(itemResult.Value);

        return itemResult;
    }

    /// <summary>
    /// Marks one of this TodoList's TodoItems as complete. The only way a TodoItem is completed.
    /// </summary>
    public Result CompleteItem(Guid todoItemId)
    {
        if (ArchivedRejection() is { } archived)
        {
            return Result.Failure(archived);
        }

        var item = _items.Find(candidate => candidate.Id == todoItemId);

        if (item is null)
        {
            return Result.Failure(DomainError.NotFound(
                "TodoList.Item.NotFound",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"No TodoItem with id '{todoItemId}' belongs to this TodoList.")));
        }

        var completeResult = item.Complete();

        if (completeResult.IsFailure)
        {
            return completeResult;
        }

        RaiseDomainEvent(new TodoItemCompletedEvent(Id, item.Id));

        return Result.Success();
    }

    /// <summary>
    /// Retires this TodoList from active use. Refused with a conflict while any TodoItem is
    /// incomplete — this is the invariant the aggregate boundary exists to protect.
    /// </summary>
    public Result Archive()
    {
        if (IsArchived)
        {
            return Result.Failure(DomainError.Conflict(
                "TodoList.AlreadyArchived",
                "The TodoList is already archived."));
        }

        if (_items.Exists(item => !item.IsComplete))
        {
            return Result.Failure(DomainError.Conflict(
                "TodoList.Archive.IncompleteItems",
                "A TodoList cannot be archived while any of its TodoItems is incomplete."));
        }

        IsArchived = true;
        ArchivedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TodoListArchivedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// The guard every mutator opens with: an archived TodoList rejects further modification.
    /// Returns the rejection, or <see langword="null"/> when the TodoList still accepts changes.
    /// </summary>
    /// <remarks>
    /// One rule enforced at more than one entry point, so it is written once — here, beside the
    /// condition that raises it — rather than copied into each mutator. Every other error in this
    /// aggregate is raised from exactly one guard and is written inline at it.
    /// </remarks>
    private DomainError? ArchivedRejection() =>
        IsArchived
            ? DomainError.Conflict("TodoList.Archived", "An archived TodoList cannot be modified.")
            : null;
}
