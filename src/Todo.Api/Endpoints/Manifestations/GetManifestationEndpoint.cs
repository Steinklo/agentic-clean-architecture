using Mediator;
using Todo.Api.Common;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.Manifestations.Queries.GetManifestation;

namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// <c>GET /api/manifestations/{manifestationId}</c> - retrieves one Manifestation.
/// </summary>
internal sealed class GetManifestationEndpoint : ManifestationEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapGet("/{manifestationId:guid}", async (
                Guid manifestationId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new GetManifestationQuery(manifestationId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToOk();
            })
            .WithName("GetManifestation")
            .WithSummary("Retrieves a Manifestation by its identity.")
            .Produces<ManifestationDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
