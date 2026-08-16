using Todo.Domain.Common;
using Todo.Domain.Manifestations;
using Todo.Domain.Manifestations.Events;

namespace Todo.UnitTests.Manifestations;

/// <summary>
/// The Manifestation aggregate, constructed directly and asserted on through the returned
/// <c>Result</c>. No mocks, no substitutes, no I/O.
/// </summary>
/// <remarks>
/// <b>This is the only seam that can reach the realized path at all.</b> No HTTP request can produce
/// a realized Manifestation, because the one adapter to reality declines every attempt - so the half
/// of the terminal-state invariant that starts from <c>Realize</c> is unreachable from the
/// integration tests by construction. That is exactly what having two seams is for: the domain rule
/// is provable here without HTTP being able to trigger it, and a state machine whose only reachable
/// path is failure would have no invariant worth enforcing.
/// </remarks>
public class ManifestationTests
{
    private static readonly Guid _todoListId = Guid.CreateVersion7();
    private static readonly Guid _todoItemId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithATodoItem_ReturnsARequestedManifestation()
    {
        var result = Manifestation.Create(_todoListId, _todoItemId);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(_todoListId, result.Value.TodoListId);
        Assert.Equal(_todoItemId, result.Value.TodoItemId);
        Assert.Equal(ManifestationState.Requested, result.Value.State);
    }

    [Fact]
    public void Create_WithATodoItem_RaisesManifestationRequestedEvent()
    {
        var manifestation = CreateManifestation();

        var requested = Assert.Single(manifestation.DomainEvents.OfType<ManifestationRequestedEvent>());

        Assert.Equal(manifestation.Id, requested.ManifestationId);
        Assert.Equal(manifestation.Id, requested.AggregateId);
        Assert.Equal(_todoListId, requested.TodoListId);
        Assert.Equal(_todoItemId, requested.TodoItemId);
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
        Assert.Equal(_todoListId, realized.TodoListId);
        Assert.Equal(_todoItemId, realized.TodoItemId);
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
        Assert.Equal(_todoListId, failed.TodoListId);
        Assert.Equal(_todoItemId, failed.TodoItemId);
    }

    /// <summary>
    /// The invariant, as a matrix: every transition out of every terminal state is refused with the
    /// same conflict, and none of the four leaves the state or the events touched.
    /// </summary>
    /// <param name="settle">How the Manifestation reached its terminal state.</param>
    /// <param name="attempt">The transition then attempted.</param>
    /// <param name="expectedState">The state it must still be in afterwards.</param>
    [Theory]
    [InlineData(ManifestationState.Realized, ManifestationState.Realized, ManifestationState.Realized)]
    [InlineData(ManifestationState.Realized, ManifestationState.Failed, ManifestationState.Realized)]
    [InlineData(ManifestationState.Failed, ManifestationState.Failed, ManifestationState.Failed)]
    [InlineData(ManifestationState.Failed, ManifestationState.Realized, ManifestationState.Failed)]
    public void Transition_FromATerminalState_IsRefusedAsAConflict(
        ManifestationState settle,
        ManifestationState attempt,
        ManifestationState expectedState)
    {
        var manifestation = CreateManifestation();

        Assert.True(Transition(manifestation, settle).IsSuccess);

        var eventsBefore = manifestation.DomainEvents.Count;

        var result = Transition(manifestation, attempt);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("Manifestation.AlreadySettled", result.Error.Code);

        // A refused transition changed nothing, and raised nothing for anyone to react to.
        Assert.Equal(expectedState, manifestation.State);
        Assert.Equal(eventsBefore, manifestation.DomainEvents.Count);
    }

    /// <summary>
    /// Both terminal states refuse with one code, because it is one rule. Written once in the
    /// aggregate, so a caller branching on it cannot be surprised by which way the Manifestation
    /// happened to settle.
    /// </summary>
    [Fact]
    public void Transition_FromEitherTerminalState_UsesTheSameCode()
    {
        var realized = CreateManifestation();
        realized.Realize();

        var failed = CreateManifestation();
        failed.Fail();

        Assert.Equal(realized.Realize().Error.Code, failed.Fail().Error.Code);
    }

    [Fact]
    public void ClearDomainEvents_AfterCreate_RemovesRaisedEvents()
    {
        var manifestation = CreateManifestation();

        manifestation.ClearDomainEvents();

        Assert.Empty(manifestation.DomainEvents);
    }

    private static Result Transition(Manifestation manifestation, ManifestationState to) =>
        to == ManifestationState.Realized ? manifestation.Realize() : manifestation.Fail();

    private static Manifestation CreateManifestation()
    {
        var result = Manifestation.Create(_todoListId, _todoItemId);

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}
