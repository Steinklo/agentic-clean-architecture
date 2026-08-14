using FluentValidation;

namespace Todo.Application.TodoLists.Queries.GetTodoList;

/// <summary>
/// Shape rules for <see cref="GetTodoListQuery"/>.
/// </summary>
/// <remarks>
/// Queries get validators too, and for the same reason commands do: an all-zero
/// <see cref="Guid"/> is a malformed request, not a request for a TodoList that happens not to
/// exist, and answering it with 404 would tell the caller the wrong thing.
/// </remarks>
public sealed class GetTodoListQueryValidator : AbstractValidator<GetTodoListQuery>
{
    /// <summary>Declares the rules.</summary>
    public GetTodoListQueryValidator() =>
        RuleFor(query => query.TodoListId).NotEmpty();
}
