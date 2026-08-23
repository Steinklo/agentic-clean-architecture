using FluentValidation;

namespace Todo.Application.Manifestations.Commands.RequestManifestation;

/// <summary>
/// Shape rules for <see cref="RequestManifestationCommand"/>.
/// </summary>
/// <remarks>
/// Both identifiers come from the route, and an all-zero <see cref="System.Guid"/> in either is a
/// malformed request rather than a request for something absent — so 400, not 404. Whether the
/// TodoItem exists is the handler's question and not this one's.
/// </remarks>
public sealed class RequestManifestationCommandValidator : AbstractValidator<RequestManifestationCommand>
{
    /// <summary>Declares the rules.</summary>
    public RequestManifestationCommandValidator()
    {
        RuleFor(command => command.TodoListId).NotEmpty();
        RuleFor(command => command.TodoItemId).NotEmpty();
    }
}
