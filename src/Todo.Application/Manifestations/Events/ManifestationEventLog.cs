using Microsoft.Extensions.Logging;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Log messages emitted by the Manifestation domain-event handlers.
/// </summary>
/// <remarks>
/// A new feature takes the next free thousand, so this block starts at 3000: 1000-1002 are the
/// pipeline behaviours and 2000-2002 are <c>TodoListEventLog</c>. The ids are what the integration
/// tests match on to prove an event was dispatched, so none of them is ever renumbered.
/// </remarks>
internal static partial class ManifestationEventLog
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "Manifestation {ManifestationId} was requested for TodoItem {TodoItemId} on TodoList {TodoListId}")]
    public static partial void ManifestationRequested(
        ILogger logger,
        Guid manifestationId,
        Guid todoItemId,
        Guid todoListId);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Manifestation {ManifestationId} was realized; completing TodoItem {TodoItemId} on TodoList {TodoListId}")]
    public static partial void ManifestationRealized(
        ILogger logger,
        Guid manifestationId,
        Guid todoItemId,
        Guid todoListId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Manifestation {ManifestationId} failed for TodoItem {TodoItemId} on TodoList {TodoListId}")]
    public static partial void ManifestationFailed(
        ILogger logger,
        Guid manifestationId,
        Guid todoItemId,
        Guid todoListId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Manifestation {ManifestationId} was realized but completing TodoItem {TodoItemId} was refused: {ErrorCode}")]
    public static partial void ManifestationCompletionRefused(
        ILogger logger,
        Guid manifestationId,
        Guid todoItemId,
        string errorCode);
}
