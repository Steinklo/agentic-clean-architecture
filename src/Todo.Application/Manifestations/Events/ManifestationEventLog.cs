using Microsoft.Extensions.Logging;

namespace Todo.Application.Manifestations.Events;

/// <summary>
/// Log messages emitted by the Manifestation domain-event handlers.
/// </summary>
/// <remarks>
/// A new feature starts a new block of event ids: 1000-1002 are the pipeline behaviours, 2000-2002
/// are <c>TodoListEventLog</c>, and this one opens at 3000. The ids are a test contract - the
/// integration tests prove an event was dispatched by matching on the id rather than the message -
/// so none of them is ever renumbered.
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
        Guid todoListId,
        Guid todoItemId);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Manifestation {ManifestationId} was realized; completing TodoItem {TodoItemId} on TodoList {TodoListId}")]
    public static partial void ManifestationRealized(
        ILogger logger,
        Guid manifestationId,
        Guid todoListId,
        Guid todoItemId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Manifestation {ManifestationId} failed for TodoItem {TodoItemId} on TodoList {TodoListId}")]
    public static partial void ManifestationFailed(
        ILogger logger,
        Guid manifestationId,
        Guid todoListId,
        Guid todoItemId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Manifestation {ManifestationId} was realized but completing TodoItem {TodoItemId} was refused: {ErrorCode}")]
    public static partial void ManifestationRealizedCompletionRefused(
        ILogger logger,
        Guid manifestationId,
        Guid todoItemId,
        string errorCode);
}
