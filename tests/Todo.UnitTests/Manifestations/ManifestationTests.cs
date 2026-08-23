using Todo.Domain.Common;
using Todo.Domain.Manifestations;
using Todo.Domain.Manifestations.Enums;
using Todo.Domain.Manifestations.Events;

namespace Todo.UnitTests.Manifestations;

/// <summary>
/// The terminal-state invariant, driven directly at the aggregate.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam that exists for exactly this case. <c>IRealityGateway</c> has no adapter and
/// always declines, so no HTTP request can ever reach <see cref="Manifestation.Realize"/> — every
/// row in the matrix below that starts from <c>Realized</c> is unreachable from the HTTP seam and
/// provable here in three lines, with no mock and no I/O.
/// </para>
/// <para>
/// The half that <em>is</em> reachable over HTTP is proved over HTTP as well, in
/// <c>FulfilManifestationEndpointTests</c>. Neither seam makes the other redundant: this one covers
/// the matrix, that one covers the wiring.
/// </para>
/// </remarks>
public class ManifestationTests
{
    [Fact]
    public void Create_WithATodoItem_ReturnsARequestedManifestation()
    {
        var todoListId = Guid.CreateVersion7();
        var todoItemId = Guid.CreateVersion7();

        var result = Manifestation.Create(todoListId, todoItemId);

        Assert.True(result.IsSuccess);
        Assert.Equal(todoListId, result.Value.TodoListId);
        Assert.Equal(todoItemId, result.Value.TodoItemId);
        Assert.Equal(ManifestationState.Requested, result.Value.State);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_WithATodoItem_RaisesManifestationRequestedEvent()
    {
        var manifestation = CreateManifestation();

        var requested = Assert.Single(manifestation.DomainEvents.OfType<ManifestationRequestedEvent>());

        Assert.Equal(manifestation.Id, requested.ManifestationId);
        Assert.Equal(manifestation.Id, requested.AggregateId);
        Assert.Equal(manifestation.TodoListId, requested.TodoListId);
        Assert.Equal(manifestation.TodoItemId, requested.TodoItemId);
    }

    [Fact]
    public void Realize_FromRequested_ReturnsSuccessAndRaisesRealizedEvent()
    {
        var manifestation = CreateManifestation();

        var result = manifestation.Realize();

        Assert.True(result.IsSuccess);
        Assert.Equal(ManifestationState.Realized, manifestation.State);

        var realized = Assert.Single(manifestation.DomainEvents.OfType<ManifestationRealizedEvent>());

        Assert.Equal(manifestation.Id, realized.ManifestationId);
        Assert.Equal(manifestation.TodoListId, realized.TodoListId);
        Assert.Equal(manifestation.TodoItemId, realized.TodoItemId);
    }

    [Fact]
    public void Fail_FromRequested_ReturnsSuccessAndRaisesFailedEvent()
    {
        var manifestation = CreateManifestation();

        var result = manifestation.Fail();

        Assert.True(result.IsSuccess);
        Assert.Equal(ManifestationState.Failed, manifestation.State);

        var failed = Assert.Single(manifestation.DomainEvents.OfType<ManifestationFailedEvent>());

        Assert.Equal(manifestation.Id, failed.ManifestationId);
        Assert.Equal(manifestation.TodoListId, failed.TodoListId);
        Assert.Equal(manifestation.TodoItemId, failed.TodoItemId);
    }

    /// <summary>
    /// The whole invariant as a matrix: every transition out of every terminal state, in both
    /// directions, refused with one code. Two of these four rows can only be reached from here.
    /// </summary>
    [Theory]
    [InlineData(ManifestationState.Realized, ManifestationState.Realized)]
    [InlineData(ManifestationState.Realized, ManifestationState.Failed)]
    [InlineData(ManifestationState.Failed, ManifestationState.Failed)]
    [InlineData(ManifestationState.Failed, ManifestationState.Realized)]
    public void Transition_FromATerminalState_ReturnsConflictAndChangesNothing(
        ManifestationState settled,
        ManifestationState attempted)
    {
        var manifestation = CreateManifestation();

        Assert.True(TransitionTo(manifestation, settled).IsSuccess);

        manifestation.ClearDomainEvents();

        var result = TransitionTo(manifestation, attempted);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("Manifestation.Terminal", result.Error.Code);

        // A refused transition settles nothing further and announces nothing.
        Assert.Equal(settled, manifestation.State);
        Assert.Empty(manifestation.DomainEvents);
    }

    private static Result TransitionTo(Manifestation manifestation, ManifestationState state) =>
        state == ManifestationState.Realized
            ? manifestation.Realize()
            : manifestation.Fail();

    private static Manifestation CreateManifestation()
    {
        var result = Manifestation.Create(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}
