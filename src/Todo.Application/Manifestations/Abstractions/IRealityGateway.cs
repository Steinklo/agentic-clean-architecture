using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations.Abstractions;

/// <summary>
/// The port through which this application asks the physical world to change.
/// </summary>
/// <remarks>
/// <para>
/// <em>Gateway</em> is the ordinary term for an adapter to an external system. The external system
/// here is reality, and there is no adapter to it — the implementation in Infrastructure declines
/// every request. That is the honest shape for any integration that has not been built: a real
/// adapter that reports it cannot do the work, rather than a stub that pretends, or a throw.
/// </para>
/// <para>
/// It answers with <see cref="Result"/> for the same reason every use case does. A thrown
/// <see cref="NotImplementedException"/> would be caught by the unhandled-exception behaviour and
/// reach the caller as a 500 — a defect where there is none. Declining is an outcome, so it is
/// returned.
/// </para>
/// <para>
/// The port lives beside the repository interface and not in <c>Common/</c>: it belongs to the
/// Manifestations feature, which is the only thing that has ever needed to address reality.
/// </para>
/// </remarks>
public interface IRealityGateway
{
    /// <summary>
    /// Asks the physical world to make the TodoItem this Manifestation refers to true.
    /// </summary>
    /// <param name="manifestation">The Manifestation being fulfilled.</param>
    /// <param name="cancellationToken">Cancels the attempt.</param>
    /// <returns>
    /// Success when reality changed; a failed <see cref="Result"/> carrying the reason otherwise.
    /// </returns>
    Task<Result> MakeTrueAsync(Manifestation manifestation, CancellationToken cancellationToken);
}
