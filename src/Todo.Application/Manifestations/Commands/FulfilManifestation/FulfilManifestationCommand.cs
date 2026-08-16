using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Domain.Common;

namespace Todo.Application.Manifestations.Commands.FulfilManifestation;

/// <summary>
/// Attempts to carry out a Manifestation - to make its TodoItem true in the physical world.
/// </summary>
/// <param name="ManifestationId">Identity of the Manifestation to fulfil.</param>
public sealed record FulfilManifestationCommand(Guid ManifestationId) : IRequest<Result>;

/// <summary>
/// Answers <see cref="FulfilManifestationCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This use case cannot succeed, and the code does not pretend otherwise.</b>
/// <see cref="IRealityGateway"/> has no adapter behind it, so every attempt comes back a failure,
/// the Manifestation settles as <c>Failed</c>, and the caller is told
/// <c>reality.not-implemented</c> as a 501. The success branch is written and reachable the day
/// something can satisfy that port; nothing here has to change for it.
/// </para>
/// <para>
/// <b>Reality is asked before the aggregate is.</b> Which transition to ask for is not known until
/// reality answers, and the alternative - this handler reading <c>State</c> and deciding for itself
/// whether the Manifestation may still settle - would be the terminal-state invariant restated
/// outside the aggregate that owns it. The cost is real and worth stating: a Manifestation that has
/// already settled still reaches the gateway before being refused. It is harmless against an
/// adapter that declines, and an adapter with side effects would want a domain-level "attempt
/// started" transition rather than this shape.
/// </para>
/// <para>
/// <b>The failure is persisted and then reported.</b> That a fulfilment was attempted and did not
/// work is a fact about the Manifestation, so it is saved; the <see cref="Result"/> handed back is
/// still the gateway's, uninspected, because deciding what its failure means is not this handler's
/// to do.
/// </para>
/// </remarks>
internal sealed class FulfilManifestationHandler(
    IManifestationRepository manifestations,
    IRealityGateway reality,
    IUnitOfWork unitOfWork)
    : IRequestHandler<FulfilManifestationCommand, Result>
{
    private readonly IManifestationRepository _manifestations = manifestations;
    private readonly IRealityGateway _reality = reality;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async ValueTask<Result> Handle(
        FulfilManifestationCommand request,
        CancellationToken cancellationToken)
    {
        var manifestation = await _manifestations
            .GetByIdAsync(request.ManifestationId, cancellationToken)
            .ConfigureAwait(false);

        if (manifestation is null)
        {
            return Result.Failure(ManifestationNotFound(request.ManifestationId));
        }

        var attempt = await _reality
            .MakeTrueAsync(manifestation, cancellationToken)
            .ConfigureAwait(false);

        var settlement = attempt.IsSuccess ? manifestation.Realize() : manifestation.Fail();

        // The aggregate refused, so nothing changed and there is nothing to commit.
        if (settlement.IsFailure)
        {
            return settlement;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attempt;
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
