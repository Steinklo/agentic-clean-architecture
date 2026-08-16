namespace Todo.Domain.Common;

/// <summary>
/// The category of a failure. Everything outside the domain — the API in particular —
/// decides how to respond by switching on this, never by parsing an error message.
/// </summary>
public enum DomainErrorType
{
    /// <summary>Input was malformed or violated a shape rule.</summary>
    Validation,

    /// <summary>A referenced thing does not exist.</summary>
    NotFound,

    /// <summary>The request is well formed but conflicts with the current state.</summary>
    Conflict,

    /// <summary>
    /// The operation is understood and legitimate, and this application cannot carry it out
    /// because the capability it needs has not been built. Distinct from <see cref="Failure"/>:
    /// nothing went wrong, and retrying changes nothing until someone implements the adapter.
    /// </summary>
    NotImplemented,

    /// <summary>Anything else that went wrong.</summary>
    Failure
}
