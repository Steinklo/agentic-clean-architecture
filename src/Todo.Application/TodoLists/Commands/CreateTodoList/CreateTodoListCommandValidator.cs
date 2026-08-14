using FluentValidation;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Application.TodoLists.Commands.CreateTodoList;

/// <summary>
/// Shape rules for <see cref="CreateTodoListCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validation happens in two layers, and keeping them apart is the point. <b>Here</b>: is the
/// request well formed - is the field present, is it a plausible size, is the identifier a
/// syntactically valid one? <b>In the domain</b>: is what it asks for allowed - the rules in
/// <see cref="TodoListTitle.Create"/> and <see cref="Todo.Domain.TodoLists.TodoList.Archive"/>
/// and their siblings.
/// </para>
/// <para>
/// So this validator rejects a missing title and an absurdly long one, and says nothing about
/// what makes a title <em>meaningful</em> - the minimum length, the trimming - because those are
/// the domain's judgement and a copy here would be a second place to change and a second place to
/// get it wrong. The upper bound is expressed as <see cref="TodoListTitle.MaxLength"/> rather than
/// a literal for the same reason: one number, owned by the domain, referenced here.
/// </para>
/// <para>
/// This class is what makes <c>ValidationBehaviour</c> real. In the repository this template comes
/// from, FluentValidation was registered and the behaviour was wired up, but no validator was ever
/// written - so the behaviour ran on every request and found nothing, for ever. A registered
/// pipeline stage with nothing in it looks exactly like a working one.
/// </para>
/// </remarks>
public sealed class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    /// <summary>Declares the rules.</summary>
    public CreateTodoListCommandValidator() =>
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoListTitle.MaxLength);
}
