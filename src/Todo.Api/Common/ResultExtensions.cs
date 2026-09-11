using Todo.Domain.Common;

namespace Todo.Api.Common;

/// <summary>
/// The one and only translation from a <see cref="Result"/> to an HTTP response.
/// </summary>
/// <remarks>
/// <para>
/// Every status code in this API is decided here. Endpoints choose the <em>shape</em> of success -
/// 200 with a body, 201 with a location, 204 with nothing - by calling the matching method below,
/// and they never see the failure path at all: the mapping from <see cref="DomainErrorType"/> to status
/// lives once, in <see cref="StatusCodeFor"/>.
/// </para>
/// <para>
/// This exists because of a specific, real defect. In the repository this template is modelled on,
/// the ladder from error category to status code was copy-pasted into every endpoint. Endpoints
/// drifted: some mapped a conflict to 400, some forgot 409 entirely, and a new error category
/// meant editing every endpoint and missing one. Adding a category is now one edit, in one switch,
/// instead of an edit to every endpoint and one of them missed.
/// </para>
/// <para>
/// <b>What this does not do, tested rather than assumed.</b> Adding a <see cref="DomainErrorType"/>
/// member is <em>not</em> a compile error here, and cannot be made into one. The switch below ends
/// in a discard, and removing it does not promote a missing category to an error - it makes this
/// switch itself fail with CS8524, "not exhaustive involving an unnamed enum value", because a
/// C# enum can hold a value no member names. <c>TreatWarningsAsErrors</c> then stops the build on a
/// switch that already handles every member there is. So a category added without an arm below
/// reaches callers as a 500 and nothing says so. Adding one means editing this switch, by hand, in
/// the same change - which is a one-line habit rather than a guarantee, and is worth knowing is a
/// habit.
/// </para>
/// </remarks>
internal static class ResultExtensions
{
    /// <summary>200 with the value, or the failure response.</summary>
    /// <typeparam name="TValue">The value type carried on success.</typeparam>
    /// <param name="result">The outcome to translate.</param>
    /// <returns>The HTTP response.</returns>
    public static IResult ToOk<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Problem(result.Error);
    }

    /// <summary>201 with the value and a Location header, or the failure response.</summary>
    /// <typeparam name="TValue">The value type carried on success.</typeparam>
    /// <param name="result">The outcome to translate.</param>
    /// <param name="location">Builds the Location header from the created value.</param>
    /// <returns>The HTTP response.</returns>
    public static IResult ToCreated<TValue>(this Result<TValue> result, Func<TValue, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);

        return result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : Problem(result.Error);
    }

    /// <summary>204, or the failure response.</summary>
    /// <param name="result">The outcome to translate.</param>
    /// <returns>The HTTP response.</returns>
    public static IResult ToNoContent(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Results.NoContent()
            : Problem(result.Error);
    }

    /// <summary>
    /// Renders a failure as RFC 9457 problem details, carrying the error's stable code as the
    /// title and its message as the detail, so a caller can act on the failure without parsing it.
    /// </summary>
    private static IResult Problem(DomainError error) =>
        Results.Problem(
            detail: error.Message,
            statusCode: StatusCodeFor(error.Type),
            title: error.Code);

    /// <summary>
    /// The status ladder. The single place in this application where an error category becomes a
    /// number.
    /// </summary>
    private static int StatusCodeFor(DomainErrorType errorType) => errorType switch
    {
        DomainErrorType.Validation => StatusCodes.Status400BadRequest,
        DomainErrorType.NotFound => StatusCodes.Status404NotFound,
        DomainErrorType.Conflict => StatusCodes.Status409Conflict,
        DomainErrorType.Failure => StatusCodes.Status500InternalServerError,

        // 501 and not 500: the request was understood and this application has no implementation
        // for it, so nothing went wrong and there is nothing to retry. Reusing Failure was the
        // alternative and was rejected - it reports a defect where there is none, and the
        // distinction disappears in any metric that counts 5xx.
        DomainErrorType.NotImplemented => StatusCodes.Status501NotImplemented,
        _ => StatusCodes.Status500InternalServerError,
    };
}
