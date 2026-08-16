using System.Globalization;
using Mediator;
using Todo.Application.Manifestations.Dtos;
using Todo.Domain.Common;

namespace Todo.Application.Manifestations.Queries.GetManifestation;

/// <summary>
/// Retrieves one Manifestation.
/// </summary>
/// <param name="ManifestationId">Identity of the Manifestation to retrieve.</param>
public sealed record GetManifestationQuery(Guid ManifestationId) : IRequest<Result<ManifestationDto>>;

/// <summary>
/// Answers <see cref="GetManifestationQuery"/>.
/// </summary>
/// <remarks>
/// A missing Manifestation is an expected outcome and comes back as a failed <see cref="Result"/>
/// carrying a NotFound-categorised <see cref="DomainError"/>, which the API turns into a 404 without
/// the endpoint deciding anything.
/// </remarks>
internal sealed class GetManifestationHandler(IManifestationRepository manifestations)
    : IRequestHandler<GetManifestationQuery, Result<ManifestationDto>>
{
    private readonly IManifestationRepository _manifestations = manifestations;

    public async ValueTask<Result<ManifestationDto>> Handle(
        GetManifestationQuery request,
        CancellationToken cancellationToken)
    {
        var manifestation = await _manifestations
            .GetByIdAsync(request.ManifestationId, cancellationToken)
            .ConfigureAwait(false);

        return manifestation is null
            ? Result.Failure<ManifestationDto>(ManifestationNotFound(request.ManifestationId))
            : Result.Success(ManifestationDto.FromDomain(manifestation));
    }

    /// <summary>
    /// This handler's own answer when there is no such aggregate, written beside the guard that
    /// raises it, carrying the one code every route for a Manifestation shares.
    /// </summary>
    private static DomainError ManifestationNotFound(Guid manifestationId) => DomainError.NotFound(
        "Manifestation.NotFound",
        string.Create(
            CultureInfo.InvariantCulture,
            $"No Manifestation with id '{manifestationId}' exists."));
}
