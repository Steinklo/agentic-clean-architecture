using FluentValidation;

namespace Todo.Application.Manifestations.Queries.GetManifestation;

/// <summary>
/// Shape rules for <see cref="GetManifestationQuery"/>.
/// </summary>
/// <remarks>
/// Queries get validators too: an all-zero <see cref="System.Guid"/> is a malformed request, not a
/// request for a Manifestation that happens not to exist, so it belongs in a 400 and not a 404.
/// </remarks>
public sealed class GetManifestationQueryValidator : AbstractValidator<GetManifestationQuery>
{
    /// <summary>Declares the rules.</summary>
    public GetManifestationQueryValidator() =>
        RuleFor(query => query.ManifestationId).NotEmpty();
}
