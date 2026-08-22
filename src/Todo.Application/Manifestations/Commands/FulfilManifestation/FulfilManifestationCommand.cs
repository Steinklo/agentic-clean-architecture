using System.Globalization;
using Mediator;
using Todo.Application.Common.Persistence;
using Todo.Application.Manifestations.Abstractions;
using Todo.Domain.Common;

namespace Todo.Application.Manifestations.Commands.FulfilManifestation;

/// <summary>
/// Attempts a Manifestation against the physical world, and records how it ended.
/// </summary>
/// <param name="ManifestationId">Identity of the Manifestation to fulfil.</param>
public sealed record FulfilManifestationCommand(Guid ManifestationId) : IRequest<Result>;

/// <summary>
/// Answers <see cref="FulfilManifestationCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the one use case in the solution that cannot succeed, because
/// <see cref="IRealityGateway"/> has no adapter. What it still does is real: the gateway's refusal
/// is recorded on the aggregate, the Manifestation is written as <c>Failed</c>, and the refusal is
/// passed to the caller uninspected — so it arrives as 501 rather than as 500, and a second attempt
/// is refused as a conflict by the domain rather than tried again.
/// </para>
/// <para>
/// <b>Why reality is asked before the state is checked.</b> Whether a Manifestation may still
/// transition is the aggregate's decision and is not restated here — this handler has no way to
/// ask, and should not have one. So it asks the gateway, then offers the outcome to the aggregate
/// and lets it refuse. The cost is a call to a gateway whose answer may be discarded; the
/// alternative is a copy of the terminal-state rule living in Application, where it would drift.
/// </para>
/// <para>
/// The failed <see cref="Result"/> from the gateway is returned exactly as received. Reading its
/// code to decide anything would be this handler taking a decision that belongs to the adapter.
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

        var transition = attempt.IsSuccess ? manifestation.Realize() : manifestation.Fail();

        // The aggregate refused to record the outcome, so there is nothing to commit. Its conflict
        // is the caller's answer.
        if (transition.IsFailure)
        {
            return transition;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Success is the gateway's success. Its refusal is returned as it arrived, still carrying
        // the category that decides the caller's status.
        return attempt;
    }

    /// <summary>
    /// This handler's own answer when there is no such aggregate, written beside the guard that
    /// raises it. Shared by every Manifestation route, so a caller sees one code.
    /// </summary>
    private static DomainError ManifestationNotFound(Guid manifestationId) => DomainError.NotFound(
        "Manifestation.NotFound",
        string.Create(
            CultureInfo.InvariantCulture,
            $"No Manifestation with id '{manifestationId}' exists."));
}
