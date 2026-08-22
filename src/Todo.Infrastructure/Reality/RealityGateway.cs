using Todo.Application.Manifestations.Abstractions;
using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Reality;

/// <summary>
/// The adapter to the physical world. It declines.
/// </summary>
/// <remarks>
/// <para>
/// This is a real adapter, not a stub and not a placeholder. It is the only implementation of
/// <see cref="IRealityGateway"/> there will be, because the external system it fronts cannot be
/// addressed from a process. What it demonstrates is the shape every not-yet-built integration
/// should take: an adapter that reports honestly that it cannot do the work, in the vocabulary the
/// rest of the application already speaks.
/// </para>
/// <para>
/// <b>It returns rather than throws, and that is the whole reason
/// <see cref="DomainErrorType.NotImplemented"/> exists.</b> A thrown
/// <see cref="NotImplementedException"/> would be caught by <c>UnhandledExceptionBehaviour</c>,
/// logged as an unhandled fault and surfaced as a 500 — telling the caller that something broke,
/// when in fact the application worked exactly as designed and the answer is simply "not
/// implemented". A returned failure carrying the <c>NotImplemented</c> category reaches the caller
/// as 501 through the one status ladder, with a stable code it can branch on.
/// </para>
/// <para>
/// Nothing below this line is refused. The Manifestation was persisted, is readable, and survives a
/// restart; only the one genuinely impossible operation declines. Refusing higher up — at the
/// repository, say — would leave everything under the API unproven, which is what this slice exists
/// to avoid.
/// </para>
/// </remarks>
internal sealed class RealityGateway : IRealityGateway
{
    /// <summary>
    /// The stable code callers branch on. Namespaced to the external system, not to an aggregate,
    /// because the refusal is the gateway's and not the domain's.
    /// </summary>
    private const string NotImplementedCode = "reality.not-implemented";

    public Task<Result> MakeTrueAsync(Manifestation manifestation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifestation);

        return Task.FromResult(Result.Failure(DomainError.NotImplemented(
            NotImplementedCode,
            "This application cannot make a TodoItem true in the physical world. There is no "
            + "adapter to reality, and there is no plan to build one.")));
    }
}
