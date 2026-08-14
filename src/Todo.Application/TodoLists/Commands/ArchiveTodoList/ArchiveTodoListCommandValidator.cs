using FluentValidation;

namespace Todo.Application.TodoLists.Commands.ArchiveTodoList;

/// <summary>
/// Shape rules for <see cref="ArchiveTodoListCommand"/>.
/// </summary>
/// <remarks>
/// The identity is required and that is all this layer has to say. Whether the TodoList may be
/// archived depends on the state of its TodoItems, which is domain knowledge and belongs to
/// <c>TodoList.Archive</c> - restating it here would give two places to change and one of them
/// would be missed.
/// </remarks>
public sealed class ArchiveTodoListCommandValidator : AbstractValidator<ArchiveTodoListCommand>
{
    /// <summary>Declares the rules.</summary>
    public ArchiveTodoListCommandValidator() =>
        RuleFor(command => command.TodoListId).NotEmpty();
}
