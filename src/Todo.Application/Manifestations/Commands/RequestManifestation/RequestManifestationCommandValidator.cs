using FluentValidation;

namespace Todo.Application.Manifestations.Commands.RequestManifestation;

/// <summary>
/// Shape rules for <see cref="RequestManifestationCommand"/>.
/// </summary>
/// <remarks>
/// Both identifiers are required and that is all this layer has to say. Whether either one names
/// something that exists is the handler's question, and it answers it with a 404 rather than a 400.
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
