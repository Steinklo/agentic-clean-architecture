using Mediator;
using Todo.Api.Common;
using Todo.Api.Endpoints.TodoLists;
using Todo.Application.Manifestations.Commands.RequestManifestation;
using Todo.Application.Manifestations.Dtos;

namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// <c>POST /api/todo-lists/{todoListId}/items/{todoItemId}/manifest</c> - records a request to make
/// a TodoItem true in the physical world.
/// </summary>
/// <remarks>
/// <para>
/// Requesting a manifestation is a named transition on a TodoItem, so it is a sub-resource of the
/// item in the same style as <c>.../complete</c> — the caller has a TodoItem and asks something of
/// it, and does not yet have a Manifestation to address.
/// </para>
/// <para>
/// <b>Why this derives from <see cref="TodoListEndpoint"/> and not from
/// <see cref="ManifestationEndpoint"/>.</b> The endpoint base states the route prefix and the
/// OpenAPI tag once, and endpoints sharing a prefix are mapped into one route group. This route
/// lives under <c>/api/todo-lists</c>, so deriving from the base that owns that prefix is what
/// keeps the statement true; deriving from the Manifestations base and overriding the prefix would
/// put two different tags on one group and let the group's tag depend on scan order. The file sits
/// with the Manifestations feature because that is the slice it belongs to — the base class it
/// derives from is a routing fact, not an ownership one.
/// </para>
/// </remarks>
internal sealed class RequestManifestationEndpoint : TodoListEndpoint
{
    /// <inheritdoc />
    public override void MapEndpoint(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group
            .MapPost("/{todoListId:guid}/items/{todoItemId:guid}/manifest", async (
                Guid todoListId,
                Guid todoItemId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender
                    .Send(new RequestManifestationCommand(todoListId, todoItemId), cancellationToken)
                    .ConfigureAwait(false);

                return result.ToCreated(manifestation => $"/api/manifestations/{manifestation.Id}");
            })
            .WithName("RequestManifestation")
            .WithSummary("Records a request to make a TodoItem true in the physical world.")
            .Produces<ManifestationDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
