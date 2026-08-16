using Mediator;
using Todo.Api.Common;
using Todo.Api.Endpoints.TodoLists;
using Todo.Application.Manifestations.Commands.RequestManifestation;
using Todo.Application.Manifestations.Dtos;

namespace Todo.Api.Endpoints.Manifestations;

/// <summary>
/// <c>POST /api/todo-lists/{todoListId}/items/{todoItemId}/manifest</c> - asks for a TodoItem to be
/// made true in the physical world.
/// </summary>
/// <remarks>
/// <para>
/// Requesting a Manifestation is a named transition on a TodoItem, so it is a sub-resource of the
/// item in the same style as <c>.../complete</c> - which is why this derives from
/// <c>TodoListEndpoint</c> and not from <see cref="ManifestationEndpoint"/> despite living beside
/// it. The base class's whole job is to state a route prefix once per prefix; an endpoint under
/// <c>/api/todo-lists</c> declaring the Manifestations prefix would put two tags on one route group
/// and leave which one wins to assembly ordering.
/// </para>
/// <para>
/// <c>Location</c> points at the new Manifestation's own address, under
/// <c>/api/manifestations</c>, because that is where it is retrievable - unlike a TodoItem, a
/// Manifestation is an aggregate root and has an address of its own.
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

                return result.ToCreated(
                    manifestation => $"{ManifestationEndpoint.Prefix}/{manifestation.Id}");
            })
            .WithName("RequestManifestation")
            .WithSummary("Asks for a TodoItem to be made true in the physical world.")
            .Produces<ManifestationDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
