using Todo.Domain.TodoLists.Entities;

namespace Todo.Application.TodoLists.Dtos;

/// <summary>
/// What a caller of the API sees of a TodoItem.
/// </summary>
/// <remarks>
/// A TodoItem is never returned on its own terms - it is reached, added and completed through its
/// TodoList - so this projection is reached the same way: nested inside
/// <see cref="TodoListDto.Items"/>, or handed back by the command that created it.
/// <para>
/// As with every DTO here the mapping is one hand-written <see cref="FromDomain"/>, and the
/// <c>TodoItemDescription</c> value object is flattened to its string.
/// </para>
/// </remarks>
/// <param name="Id">Identity of the TodoItem.</param>
/// <param name="Description">What it asks someone to do, trimmed and validated by the domain.</param>
/// <param name="IsComplete">Whether it has been completed.</param>
/// <param name="CreatedAt">When it was created.</param>
/// <param name="CompletedAt">When it was completed, if it has been.</param>
public sealed record TodoItemDto(
    Guid Id,
    string Description,
    bool IsComplete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>
    /// Projects a TodoItem onto its API representation.
    /// </summary>
    /// <param name="todoItem">The item to project.</param>
    /// <returns>The DTO.</returns>
    public static TodoItemDto FromDomain(TodoItem todoItem)
    {
        ArgumentNullException.ThrowIfNull(todoItem);

        return new TodoItemDto(
            todoItem.Id,
            todoItem.Description.Value,
            todoItem.IsComplete,
            todoItem.CreatedAt,
            todoItem.CompletedAt);
    }
}
