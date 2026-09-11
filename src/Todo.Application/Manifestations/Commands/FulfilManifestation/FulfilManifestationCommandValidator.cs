using FluentValidation;

namespace Todo.Application.Manifestations.Commands.FulfilManifestation;

/// <summary>
/// Shape rules for <see cref="FulfilManifestationCommand"/>.
/// </summary>
/// <remarks>
/// An all-zero <see cref="System.Guid"/> is a malformed request and belongs in a 400, which is also
/// what keeps it from reaching the gateway and coming back as 501 — a shape failure must not be
/// reported as an unimplemented feature.
/// </remarks>
public sealed class FulfilManifestationCommandValidator : AbstractValidator<FulfilManifestationCommand>
{
    /// <summary>Declares the rules.</summary>
    public FulfilManifestationCommandValidator() =>
        RuleFor(command => command.ManifestationId).NotEmpty();
}
