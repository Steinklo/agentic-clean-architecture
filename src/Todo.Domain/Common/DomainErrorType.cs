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

    /// <summary>Anything else that went wrong.</summary>
    Failure,

    /// <summary>
    /// The request is understood and legitimate, and this system has no implementation for it.
    /// Distinct from <see cref="Failure"/>: nothing went wrong, so the caller has nothing to retry
    /// and no reason to suspect a defect. Reserved for an adapter that honestly reports it cannot
    /// do the work, never for one that failed while trying.
    /// <para>
    /// Declared last so that adding it renumbers nothing. Members are appended here, never
    /// inserted, even though nothing persists or serialises the underlying value today.
    /// </para>
    /// </summary>
    NotImplemented
}
