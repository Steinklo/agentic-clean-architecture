using FluentValidation;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Application.TodoLists.Commands.AddTodoItem;

/// <summary>
/// Shape rules for <see cref="AddTodoItemCommand"/>.
/// </summary>
/// <remarks>
/// Shape only, as always: the identifier must be a real one and the description must be present and
/// no longer than the domain's maximum. The minimum length and the trimming are
/// <see cref="TodoItemDescription.Create"/>'s rules and are deliberately not restated here - one
/// number, owned by the domain, referenced from here.
/// </remarks>
public sealed class AddTodoItemCommandValidator : AbstractValidator<AddTodoItemCommand>
{
    /// <summary>Declares the rules.</summary>
    public AddTodoItemCommandValidator()
    {
        RuleFor(command => command.TodoListId).NotEmpty();

        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(TodoItemDescription.MaxLength);
    }
}
