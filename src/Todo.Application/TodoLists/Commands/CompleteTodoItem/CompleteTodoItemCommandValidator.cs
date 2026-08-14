using FluentValidation;

namespace Todo.Application.TodoLists.Commands.CompleteTodoItem;

/// <summary>
/// Shape rules for <see cref="CompleteTodoItemCommand"/>.
/// </summary>
/// <remarks>
/// Both identifiers are required for the same reason a query's is: an all-zero <see cref="Guid"/>
/// is a malformed request, not a request for something that happens not to exist, and answering it
/// with a not-found would tell the caller the wrong thing.
/// </remarks>
public sealed class CompleteTodoItemCommandValidator : AbstractValidator<CompleteTodoItemCommand>
{
    /// <summary>Declares the rules.</summary>
    public CompleteTodoItemCommandValidator()
    {
        RuleFor(command => command.TodoListId).NotEmpty();
        RuleFor(command => command.TodoItemId).NotEmpty();
    }
}
