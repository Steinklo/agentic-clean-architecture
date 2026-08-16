using System.Globalization;
using Todo.Application.Manifestations;
using Todo.Domain.Common;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Reality;

/// <summary>
/// The adapter to reality. It declines.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a real adapter, not a stub.</b> Everything above it - the aggregate, the use cases,
/// the repository, the schema, the endpoints - is finished and works. The one thing that does not is
/// the integration with the external system, because there is no external system to integrate with,
/// and this reports that honestly rather than pretending to have tried.
/// </para>
/// <para>
/// <b>It returns; it does not throw.</b> A <see cref="NotImplementedException"/> would be caught by
/// <c>UnhandledExceptionBehaviour</c> and reach the caller as a 500 - "this application broke" -
/// when the truth is "this application has never been able to do that". Returning
/// <see cref="DomainError.NotImplemented"/> reaches the caller as a 501 carrying a code they can
/// branch on, which is the correct shape for any not-yet-built integration and has nothing to do
/// with the subject matter being absurd.
/// </para>
/// <para>
/// The day something can satisfy this port, this class is the only thing that changes. Nothing above
/// it knows the difference.
/// </para>
/// </remarks>
internal sealed class RealityGateway : IRealityGateway
{
    public Task<Result> MakeTrueAsync(Manifestation manifestation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifestation);

        return Task.FromResult(Result.Failure(DomainError.NotImplemented(
            "reality.not-implemented",
            string.Create(
                CultureInfo.InvariantCulture,
                $"There is no adapter to the physical world, so TodoItem '{manifestation.TodoItemId}' cannot be made true. This application has never been able to do that."))));
    }
}
