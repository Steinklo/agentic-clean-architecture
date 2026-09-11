using Mediator;
using Todo.Api.Common;
using Todo.Application.Manifestations.Commands.FulfilManifestation;

namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// <c>POST /api/manifestations/{manifestationId}/fulfil</c> - attempts a Manifestation against the
/// physical world.
/// </summary>
/// <remarks>
/// <para>
/// The one endpoint in this API that cannot succeed. It is written exactly like every other one —
/// bind, send, translate — and names no status code outside its OpenAPI metadata. The 501 it
/// returns is decided by the <c>NotImplemented</c> category the gateway's refusal carries, through
/// the single ladder in <see cref="ResultExtensions"/>, which is what makes it evidence that the
/// ladder still holds when a category is added.
/// </para>
/// <para>
/// The 409 is the second attempt: the first records the refusal and settles the Manifestation as
/// failed, and the domain then refuses to settle it again.
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
            .WithSummary("Attempts a Manifestation against the physical world.")

            // No success is declared, deliberately. The code above chooses 204 as its success
            // shape - that is what ToNoContent() means - but no request can reach it while
            // IRealityGateway declines every attempt. Advertising a 204 would promise callers a
            // response that will not arrive. If an adapter to reality is ever built, this line
            // comes back with it.
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status501NotImplemented);
    }
}
