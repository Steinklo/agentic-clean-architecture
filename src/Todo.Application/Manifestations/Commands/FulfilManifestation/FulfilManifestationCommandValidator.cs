using FluentValidation;

namespace Todo.Application.Manifestations.Commands.FulfilManifestation;

/// <summary>
/// Shape rules for <see cref="FulfilManifestationCommand"/>.
/// </summary>
/// <remarks>
/// The identity is required and that is all. Whether the Manifestation may still be fulfilled is
/// the aggregate's rule and belongs to <c>Manifestation.Fail</c> and <c>Manifestation.Realize</c>.
/// </remarks>
public sealed class FulfilManifestationCommandValidator : AbstractValidator<FulfilManifestationCommand>
{
    /// <summary>Declares the rules.</summary>
    public FulfilManifestationCommandValidator() =>
        RuleFor(command => command.ManifestationId).NotEmpty();
}
