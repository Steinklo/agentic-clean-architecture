namespace Todo.Api.Endpoints.TodoLists;

/// <summary>
/// Base for every endpoint of the TodoLists feature.
/// </summary>
/// <remarks>
/// The prefix and the tag are stated once, here, rather than repeated on each endpoint - so the
/// feature cannot end up half under one route and half under another. A new endpoint on this
/// feature derives from this class and implements one method.
/// </remarks>
internal abstract class TodoListEndpoint : IEndpoint
{
    /// <inheritdoc />
    public string GroupPrefix => "/api/todo-lists";

    /// <inheritdoc />
    public string GroupTag => "TodoLists";

    /// <inheritdoc />
    public abstract void MapEndpoint(RouteGroupBuilder group);
}
