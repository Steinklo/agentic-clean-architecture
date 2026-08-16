using FluentValidation;

namespace Todo.Application.Manifestations.Queries.GetManifestation;

/// <summary>
/// Shape rules for <see cref="GetManifestationQuery"/>.
/// </summary>
/// <remarks>
/// A query gets a validator for the same reason a command does: an all-zero <see cref="Guid"/> is a
/// malformed request, not a request for something that happens not to exist, so it belongs in a 400
/// and not a 404.
/// </remarks>
public sealed class GetManifestationQueryValidator : AbstractValidator<GetManifestationQuery>
{
    /// <summary>Declares the rules.</summary>
    public GetManifestationQueryValidator() =>
        RuleFor(query => query.ManifestationId).NotEmpty();
}
