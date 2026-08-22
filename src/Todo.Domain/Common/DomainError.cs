namespace Todo.Domain.Common;

/// <summary>
/// A structured failure: a stable machine-readable <see cref="Code"/>, a human-readable
/// <see cref="Message"/>, and a <see cref="DomainErrorType"/> the transport layer maps to a status.
/// Errors are constructed through the factory per category so the category is never guessed.
/// </summary>
public sealed record DomainError
{
    private DomainError(string code, string message, DomainErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>Stable identifier for this failure, e.g. <c>TodoList.Archive.IncompleteItem</c>.</summary>
    public string Code { get; }

    /// <summary>Human-readable description of what went wrong.</summary>
    public string Message { get; }

    /// <summary>The category this failure belongs to.</summary>
    public DomainErrorType Type { get; }

    /// <summary>Input was malformed or violated a shape rule.</summary>
    public static DomainError Validation(string code, string message) => new(code, message, DomainErrorType.Validation);

    /// <summary>A referenced thing does not exist.</summary>
    public static DomainError NotFound(string code, string message) => new(code, message, DomainErrorType.NotFound);

    /// <summary>The request conflicts with the current state of the aggregate.</summary>
    public static DomainError Conflict(string code, string message) => new(code, message, DomainErrorType.Conflict);

    /// <summary>Anything else that went wrong.</summary>
    public static DomainError Failure(string code, string message) => new(code, message, DomainErrorType.Failure);

    /// <summary>
    /// The operation is legitimate and this system has no implementation for it. An adapter with
    /// nothing behind it returns this rather than throwing <see cref="NotImplementedException"/>:
    /// a thrown exception is caught by the unhandled-exception behaviour and surfaces as a 500,
    /// which tells the caller a defect occurred when nothing went wrong.
    /// </summary>
    public static DomainError NotImplemented(string code, string message) =>
        new(code, message, DomainErrorType.NotImplemented);

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}
