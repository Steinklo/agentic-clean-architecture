using System.Globalization;
using Todo.Domain.Common;
using Todo.Domain.Manifestations.Enums;
using Todo.Domain.Manifestations.Events;

namespace Todo.Domain.Manifestations;

/// <summary>
/// The aggregate root: a request to make a TodoItem true in the physical world, and a record of how
/// that request ended. The invariant that justifies the boundary is finality — a Manifestation that
/// has reached a terminal state never leaves it.
/// </summary>
/// <remarks>
/// <para>
/// The TodoItem it refers to is held <em>by id</em>, as a pair of <see cref="Guid"/>s, and never as
/// a navigation. A Manifestation and a TodoList are two aggregates: each is loaded, changed and
/// saved on its own, and the only thing that travels between them is an event. A navigation here
/// would let one transaction change both, which is the boundary dissolving quietly.
/// </para>
/// <para>
/// The two ids travel together because a TodoItem is only reachable through its TodoList — the
/// TodoItem id alone would not let anything load it.
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

    /// <summary>The TodoList owning the TodoItem this Manifestation refers to.</summary>
    public Guid TodoListId { get; private set; }

    /// <summary>The TodoItem this Manifestation would make true.</summary>
    public Guid TodoItemId { get; private set; }

    /// <summary>How far this Manifestation has got.</summary>
    public ManifestationState State { get; private set; }

    /// <summary>
    /// Records a request to manifest a TodoItem. Whether that TodoItem exists is settled before
    /// this is called, by the use case that holds its TodoList.
    /// </summary>
    /// <remarks>
    /// Answers with a <see cref="Result{TValue}"/> like every other aggregate factory here, and
    /// there is currently no input it refuses: a Manifestation carries no value object, and the two
    /// ids it holds are checked for shape by the request's validator and for existence by the
    /// handler. The shape is the aggregate factory's contract rather than a claim that this can
    /// fail, and keeping it means a rule added later is added at the guard rather than at every
    /// call site.
    /// </remarks>
    /// <param name="todoListId">The TodoList owning the TodoItem.</param>
    /// <param name="todoItemId">The TodoItem to make true.</param>
    /// <returns>The new Manifestation.</returns>
    public static Result<Manifestation> Create(Guid todoListId, Guid todoItemId)
    {
        var manifestation = new Manifestation(Guid.CreateVersion7(), todoListId, todoItemId);

        manifestation.RaiseDomainEvent(new ManifestationRequestedEvent(
            manifestation.Id,
            manifestation.TodoListId,
            manifestation.TodoItemId));

        return Result.Success(manifestation);
    }

    /// <summary>
    /// Records that reality changed: the TodoItem is now true. Refused with a conflict once this
    /// Manifestation is terminal.
    /// </summary>
    /// <remarks>
    /// Nothing in this solution can reach this from outside the domain, because
    /// <c>IRealityGateway</c> has no adapter and always declines. It is modelled and tested anyway
    /// — a state machine with only a failure path has no finality worth enforcing, and the
    /// aggregate seam can drive this where HTTP cannot.
    /// </remarks>
    /// <returns>Success, or the terminal-state conflict.</returns>
    public Result Realize()
    {
        if (TerminalRejection() is { } terminal)
        {
            return Result.Failure(terminal);
        }

        State = ManifestationState.Realized;
        RaiseDomainEvent(new ManifestationRealizedEvent(Id, TodoListId, TodoItemId));

        return Result.Success();
    }

    /// <summary>
    /// Records that reality declined, or could not be reached. Refused with a conflict once this
    /// Manifestation is terminal.
    /// </summary>
    /// <returns>Success, or the terminal-state conflict.</returns>
    public Result Fail()
    {
        if (TerminalRejection() is { } terminal)
        {
            return Result.Failure(terminal);
        }

        State = ManifestationState.Failed;
        RaiseDomainEvent(new ManifestationFailedEvent(Id, TodoListId, TodoItemId));

        return Result.Success();
    }

    /// <summary>
    /// The guard both transitions open with: a settled Manifestation does not resettle. Returns the
    /// rejection, or <see langword="null"/> while the outcome is still open.
    /// </summary>
    /// <remarks>
    /// One rule rejecting at two entry points, so it is written once — here, beside the condition
    /// that raises it — rather than copied into <see cref="Realize"/> and <see cref="Fail"/>. That
    /// also makes the code a single fact: a caller who sees <c>Manifestation.Terminal</c> knows the
    /// outcome was already settled without needing to know which way.
    /// </remarks>
    private DomainError? TerminalRejection() =>
        State == ManifestationState.Requested
            ? null
            : DomainError.Conflict(
                "Manifestation.Terminal",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The Manifestation is already {State} and cannot change state again."));
}
