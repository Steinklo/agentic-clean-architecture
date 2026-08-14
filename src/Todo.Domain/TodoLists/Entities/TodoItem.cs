using Todo.Domain.Common;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Domain.TodoLists.Entities;

/// <summary>
/// A single unit of work belonging to exactly one TodoList. It has identity, but it can only be
/// created or changed through its TodoList — which is why its constructor and mutators are internal.
/// </summary>
public sealed class TodoItem : Entity<Guid>
{
    private TodoItem(Guid id, Guid todoListId, TodoItemDescription description)
        : base(id)
    {
        TodoListId = todoListId;
        Description = description;
    }

    /// <summary>The TodoList this item belongs to.</summary>
    public Guid TodoListId { get; private set; }

    /// <summary>What this item asks someone to do.</summary>
    public TodoItemDescription Description { get; private set; }

    /// <summary>Whether the item has been completed.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>When the item was completed, if it has been.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    internal static Result<TodoItem> Create(Guid todoListId, string description)
    {
        var descriptionResult = TodoItemDescription.Create(description);

        return descriptionResult.IsFailure
            ? Result.Failure<TodoItem>(descriptionResult.Error)
            : Result.Success(new TodoItem(Guid.CreateVersion7(), todoListId, descriptionResult.Value));
    }

    internal Result Complete()
    {
        if (IsComplete)
        {
            return Result.Failure(DomainError.Conflict(
                "TodoList.Item.AlreadyComplete",
                "The TodoItem is already complete."));
        }

        IsComplete = true;
        CompletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
