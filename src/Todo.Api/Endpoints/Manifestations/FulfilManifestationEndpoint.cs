using Mediator;
using Todo.Api.Common;
using Todo.Application.Manifestations.Commands.FulfilManifestation;

namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// <c>POST /api/manifestations/{manifestationId}/fulfil</c> - attempts to carry out a Manifestation.
/// </summary>
/// <remarks>
/// <para>
/// The one endpoint in this solution that cannot succeed. Fulfilment needs an adapter to the
/// physical world and there is not one, so every call comes back 501 carrying
/// <c>reality.not-implemented</c>.
/// </para>
/// <para>
/// That 501 is decided nowhere near here. The gateway returns a <c>NotImplemented</c>-categorised
/// <c>DomainError</c> and the single ladder in <c>ResultExtensions</c> turns the category into the
/// number, exactly as it does for 400, 404 and 409 - the status below appears only as OpenAPI
/// metadata. The success shape is <c>ToNoContent()</c> and stays written, because the day an adapter
/// exists this endpoint is already correct.
/// </para>
/// </remarks>
internal sealed class FulfilManifestationEndpoint : ManifestationEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/{manifestationId:guid}/fulfil", async (
                Guid manifestationId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new FulfilManifestationCommand(manifestationId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToNoContent();
            })
            .WithName("FulfilManifestation")
            .WithSummary("Attempts to make a Manifestation's TodoItem true in the physical world.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status501NotImplemented);
    }
}
