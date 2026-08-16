using System.Globalization;
using Todo.Domain.Common;
using Todo.Domain.Manifestations.Events;

namespace Todo.Domain.Manifestations;

/// <summary>
/// The aggregate root: a record that someone asked for a TodoItem to be made true in the physical
/// world, and what became of that request.
/// </summary>
/// <remarks>
/// <para>
/// <b>It references its TodoItem by id, never by navigation.</b> A TodoList and a Manifestation are
/// two aggregates, so one transaction changes one of them; holding a <c>TodoItem</c> reference here
/// would make it possible - and then eventually normal - to load both and save them together, which
/// is the boundary dissolving one convenient call at a time. Whether the TodoItem exists is checked
/// by the use case that creates the Manifestation, because it is the only thing that can load the
/// other aggregate.
/// </para>
/// <para>
/// <b>The invariant: terminal states are final.</b> <see cref="ManifestationState.Realized"/> and
/// <see cref="ManifestationState.Failed"/> are terminal, so a Manifestation settles exactly once and
/// its outcome cannot be rewritten by a second caller. Both transitions reject on that one
/// condition, so it is written once in <see cref="TerminalRejection"/>.
/// </para>
/// </remarks>
public sealed class Manifestation : AggregateRoot<Guid>
{
    private Manifestation(Guid id, Guid todoListId, Guid todoItemId)
        : base(id)
    {
        TodoListId = todoListId;
        TodoItemId = todoItemId;
        State = ManifestationState.Requested;
    }

    /// <summary>The TodoList owning the TodoItem this Manifestation is about.</summary>
    public Guid TodoListId { get; private set; }

    /// <summary>The TodoItem this Manifestation asks to be made true.</summary>
    public Guid TodoItemId { get; private set; }

    /// <summary>How far this Manifestation has got.</summary>
    public ManifestationState State { get; private set; }

    /// <summary>
    /// Records a request to make a TodoItem true in the physical world. It starts
    /// <see cref="ManifestationState.Requested"/> and nothing else can create one.
    /// </summary>
    /// <param name="todoListId">The TodoList owning the TodoItem.</param>
    /// <param name="todoItemId">The TodoItem to be made true.</param>
    /// <returns>The new Manifestation.</returns>
    public static Result<Manifestation> Create(Guid todoListId, Guid todoItemId)
    {
        var manifestation = new Manifestation(Guid.CreateVersion7(), todoListId, todoItemId);

        manifestation.RaiseDomainEvent(
            new ManifestationRequestedEvent(manifestation.Id, todoListId, todoItemId));

        return Result.Success(manifestation);
    }

    /// <summary>
    /// Records that the TodoItem was made true. Refused with a conflict once this Manifestation has
    /// settled.
    /// </summary>
    /// <remarks>
    /// Nothing over HTTP reaches this, and that is not an oversight: the only adapter to reality
    /// declines, so no request can ever produce a realized Manifestation. It is modelled and tested
    /// at the aggregate seam because a state machine with only a failure path has no invariant worth
    /// enforcing - and because the day an adapter exists, the rule it has to obey is already here
    /// and already proven.
    /// </remarks>
    /// <returns>Success, or the conflict refusing a second settlement.</returns>
    public Result Realize()
    {
        if (TerminalRejection() is { } settled)
        {
            return Result.Failure(settled);
        }

        State = ManifestationState.Realized;
        RaiseDomainEvent(new ManifestationRealizedEvent(Id, TodoListId, TodoItemId));

        return Result.Success();
    }

    /// <summary>
    /// Records that the attempt to make the TodoItem true did not succeed. Refused with a conflict
    /// once this Manifestation has settled.
    /// </summary>
    /// <returns>Success, or the conflict refusing a second settlement.</returns>
    public Result Fail()
    {
        if (TerminalRejection() is { } settled)
        {
            return Result.Failure(settled);
        }

        State = ManifestationState.Failed;
        RaiseDomainEvent(new ManifestationFailedEvent(Id, TodoListId, TodoItemId));

        return Result.Success();
    }

    /// <summary>
    /// The guard both transitions open with: a settled Manifestation has no further transitions.
    /// Returns the rejection, or <see langword="null"/> while it can still settle.
    /// </summary>
    /// <remarks>
    /// One rule enforced at two entry points, so it is written once - the same shape as
    /// <c>TodoList.ArchivedRejection()</c>, and for the same reason: two copies is two places to
    /// change and one of them missed.
    /// </remarks>
    private DomainError? TerminalRejection() =>
        State is ManifestationState.Realized or ManifestationState.Failed
            ? DomainError.Conflict(
                "Manifestation.AlreadySettled",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The Manifestation is already {State} and cannot change state again."))
            : null;
}
