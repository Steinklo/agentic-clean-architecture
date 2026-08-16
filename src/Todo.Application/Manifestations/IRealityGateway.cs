using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Application.Manifestations;

/// <summary>
/// The adapter to the external system a Manifestation ultimately depends on. The external system is
/// reality.
/// </summary>
/// <remarks>
/// <para>
/// <em>Gateway</em> is the ordinary term for an adapter to something outside the application, and it
/// is declared here beside <see cref="IManifestationRepository"/> for the ordinary reason: the layer
/// that needs the capability owns the contract, and whatever can satisfy it lives further out.
/// </para>
/// <para>
/// <b>It answers with a <see cref="Result"/> and never throws.</b> An implementation that cannot do
/// the work returns <c>DomainError.NotImplemented</c>, which the API renders as 501. Throwing
/// <see cref="NotImplementedException"/> instead would be caught by
/// <c>UnhandledExceptionBehaviour</c> and reach the caller as a 500 - telling them the application
/// broke, when what actually happened is that it never offered this. That distinction is the whole
/// reason the <c>NotImplemented</c> category exists, and it survives only while adapters report
/// rather than throw.
/// </para>
/// </remarks>
public interface IRealityGateway
{
    /// <summary>
    /// Attempts to make the TodoItem this Manifestation refers to true in the physical world.
    /// </summary>
    /// <param name="manifestation">The Manifestation to act on.</param>
    /// <param name="cancellationToken">Cancels the attempt.</param>
    /// <returns>Success when reality now agrees, or the failure explaining why it does not.</returns>
    Task<Result> MakeTrueAsync(Manifestation manifestation, CancellationToken cancellationToken);
}
