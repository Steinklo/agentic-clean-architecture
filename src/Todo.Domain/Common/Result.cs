namespace Todo.Domain.Common;

/// <summary>
/// The outcome of an operation that either succeeded or failed with a structured <see cref="DomainError"/>.
/// Expected failures are returned, never thrown.
/// </summary>
public class Result
{
    private readonly DomainError? _error;

    /// <summary>
    /// Creates a result. Use the static factories; this exists so <see cref="Result{TValue}"/> can chain to it.
    /// </summary>
    protected Result(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentNullException(nameof(error), "A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed and <see cref="Error"/> is available.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The failure. Reading this on a successful result is a programming error, not a runtime condition,
    /// so it throws rather than returning null — which is what lets callers use it without a null check.
    /// </summary>
    public DomainError Error =>
        _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>A success carrying no value.</summary>
    public static Result Success() => new(true, null);

    /// <summary>A success carrying <paramref name="value"/>.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    /// <summary>A failure carrying <paramref name="error"/>.</summary>
    public static Result Failure(DomainError error) => new(false, error);

    /// <summary>A failure of a value-bearing operation, carrying <paramref name="error"/>.</summary>
    public static Result<TValue> Failure<TValue>(DomainError error) => new(default, false, error);
}

/// <summary>
/// The outcome of an operation that produces a <typeparamref name="TValue"/> on success.
/// </summary>
/// <typeparam name="TValue">The type produced on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, DomainError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The produced value. Declared non-nullable and guarded here so that call sites never need the
    /// null-forgiving operator: check <see cref="Result.IsSuccess"/>, then read this.
    /// </summary>
    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException($"A failed result has no value. Error: {Error}");
}
