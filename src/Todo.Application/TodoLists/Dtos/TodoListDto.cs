using Todo.Domain.TodoLists;

namespace Todo.Application.TodoLists.Dtos;

/// <summary>
/// What a caller of the API sees of a TodoList.
/// </summary>
/// <remarks>
/// Mapping is one hand-written <see cref="FromDomain"/> per DTO - no AutoMapper, no Mapster.
/// A convention-based mapper turns a rename in the domain into a silently null field at runtime,
/// which is exactly the class of bug the domain model's encapsulation exists to prevent. This way
/// the same rename is a compile error, and the projection is readable without knowing a mapping
/// library's rules.
/// <para>
/// Value objects are flattened to their primitive here: the aggregate is the only thing that gets
/// to hold a <c>TodoListTitle</c>, and callers get a string.
/// </para>
/// <para>
/// The TodoItems travel with the TodoList rather than having a projection of their own, because
/// the aggregate is loaded and saved whole and a TodoItem outside its TodoList is not a thing this
/// API has.
/// </para>
/// </remarks>
/// <param name="Id">Identity of the TodoList.</param>
/// <param name="Title">Its title, trimmed and validated by the domain.</param>
/// <param name="IsArchived">Whether it has been retired from active use.</param>
/// <param name="CreatedAt">When it was created.</param>
/// <param name="ArchivedAt">When it was archived, if it has been.</param>
/// <param name="Items">The TodoItems it currently holds.</param>
public sealed record TodoListDto(
    Guid Id,
    string Title,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt,
    IReadOnlyList<TodoItemDto> Items)
{
    /// <summary>
    /// Projects a TodoList aggregate onto its API representation.
    /// </summary>
    /// <param name="todoList">The aggregate to project.</param>
    /// <returns>The DTO.</returns>
    public static TodoListDto FromDomain(TodoList todoList)
    {
        ArgumentNullException.ThrowIfNull(todoList);

        return new TodoListDto(
            todoList.Id,
            todoList.Title.Value,
            todoList.IsArchived,
            todoList.CreatedAt,
            todoList.ArchivedAt,
            [.. todoList.Items.Select(TodoItemDto.FromDomain)]);
    }
}
