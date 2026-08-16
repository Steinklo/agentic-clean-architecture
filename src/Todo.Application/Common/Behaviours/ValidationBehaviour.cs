using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Todo.Domain.Common;

namespace Todo.Application.Common.Behaviours;

/// <summary>
/// Runs every <see cref="IValidator{T}"/> registered for the request and, if any fails, short
/// circuits the pipeline with a Validation-categorised failure <see cref="Result"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two things about this behaviour are deliberate, and both fix defects seen in the repository
/// this template is modelled on.
/// </para>
/// <para>
/// It <em>returns</em> rather than throws. A rejected request is an expected outcome of a public
/// API, not an exceptional one, so it travels the same way as every other expected failure - as a
/// <see cref="Result"/> carrying a <see cref="DomainError"/> whose <see cref="DomainError.Type"/> the API
/// turns into a status code. Nothing needs an exception filter to produce a 400.
/// </para>
/// <para>
/// It is also constrained, via <typeparamref name="TResponse"/>, to requests that answer with a
/// <see cref="Result"/>. That is what lets it return a failure at all, and Mediator's source
/// generator honours the constraint - the behaviour is only woven into pipelines it fits.
/// </para>
/// <para>
/// The behaviour being wired up is not the same thing as it doing anything: with no validator
/// registered for a request it is a no-op. See <c>CreateTodoListCommandValidator</c> for the shape
/// rules it actually enforces, and <c>ConfigureServices</c> for the assembly scan that finds them.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The request being validated.</typeparam>
/// <typeparam name="TResponse">The <see cref="Result"/> the handler returns.</typeparam>
internal sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
    where TResponse : Result
{
    /// <summary>
    /// Stable code for every shape failure. The individual failures are in the message; this
    /// identifies the category so a caller can branch on it without parsing prose.
    /// </summary>
    private const string ValidationFailedCode = "Validation.Failed";

    /// <summary>
    /// Builds the failed <typeparamref name="TResponse"/>. Resolved once per closed generic type,
    /// because a static field on a generic type exists once per type argument combination - so the
    /// reflection below runs at most once per request type, not once per request.
    /// </summary>
    private static readonly Func<DomainError, TResponse> _failureFactory = BuildFailureFactory();

    private readonly IValidator<TRequest>[] _validators = [.. validators];

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next(message, cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(message);

        var results = await Task
            .WhenAll(_validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(result => result.Errors).ToArray();

        return failures.Length == 0
            ? await next(message, cancellationToken).ConfigureAwait(false)
            : _failureFactory(ToError(failures));
    }

    /// <summary>
    /// Collapses the failures into one <see cref="DomainError"/>, keeping every message the caller needs
    /// in order to fix the request.
    /// </summary>
    private static DomainError ToError(IEnumerable<ValidationFailure> failures) =>
        DomainError.Validation(
            ValidationFailedCode,
            string.Join(
                " ",
                failures
                    .Select(failure => failure.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)));

    /// <summary>
    /// <see cref="Result.Failure{TValue}(DomainError)"/> is generic in the value type, which is only
    /// known here as <typeparamref name="TResponse"/>, so the closed method is bound reflectively
    /// once and reused.
    /// </summary>
    private static Func<DomainError, TResponse> BuildFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return error => (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];

        var failure = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition)
            .MakeGenericMethod(valueType);

        return error => (TResponse)failure.Invoke(null, [error])!;
    }
}
