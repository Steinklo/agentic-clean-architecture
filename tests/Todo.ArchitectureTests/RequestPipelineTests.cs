using FluentValidation;
using Todo.Domain.Common;

namespace Todo.ArchitectureTests;

/// <summary>
/// The request pipeline, and the three ways a slice can be wired up wrongly without anything
/// going red.
/// </summary>
/// <remarks>
/// Every rule here guards a <em>silent</em> failure. The build stays green, dependency injection
/// resolves, the endpoint answers, and a piece of the pipeline simply never runs. None of them is
/// visible from the HTTP seam unless someone thought to write the one test that would expose it,
/// which is exactly why they belong in a suite that cannot be forgotten.
/// </remarks>
public sealed class RequestPipelineTests
{
    /// <summary>
    /// Rule: <see cref="Rules.RequestsRespondWithResult"/>. The dead-validation defect, in its
    /// purest form: <c>ValidationBehaviour</c> is declared
    /// <c>where TResponse : Result</c>, so a request answering with anything else is not
    /// <em>rejected</em> by the pipeline, it is skipped by it.
    /// </summary>
    [Fact]
    public void Requests_EveryOne_AnswersWithResult() =>
        Rule.OverTypes(
            Rules.RequestsRespondWithResult,
            Requests.All(),
            type =>
            {
                var response = ResponseTypeOf(type);
                return typeof(Result).IsAssignableFrom(response)
                    ? null
                    : $"answers with {response?.Name ?? "nothing"}, which is not a Result. "
                      + "ValidationBehaviour is constrained 'where TResponse : Result', so this "
                      + "request's validator is registered and never runs.";
            });

    /// <summary>
    /// Rule: <see cref="Rules.ValidatorsArePublic"/>. Accessibility is the registration here, so
    /// this is one keyword between a validator that runs and one that does not.
    /// </summary>
    [Fact]
    public void Validators_EveryOne_IsPublic() =>
        Rule.OverTypes(
            Rules.ValidatorsArePublic,
            Validators(),
            type => type.IsPublic
                ? null
                : "is not public. AddValidatorsFromAssemblyContaining scans with "
                  + "includeInternalTypes at its default of false, so this validator registers "
                  + "nothing and never runs.");

    /// <summary>
    /// Rule: <see cref="Rules.EveryRequestHasAValidator"/>. A query needs one too — an all-zero
    /// <see cref="Guid"/> is a malformed request rather than a request for something absent, so
    /// it belongs in a 400 and not a 404.
    /// </summary>
    [Fact]
    public void Requests_EveryOne_HasAValidator()
    {
        var validated = Validators()
            .Select(ValidatedTypeOf)
            .Where(type => type is not null)
            .ToHashSet();

        Rule.OverTypes(
            Rules.EveryRequestHasAValidator,
            Requests.All(),
            type => validated.Contains(type)
                ? null
                : $"has no AbstractValidator<{type.Name}>. Malformed input reaches the domain "
                  + "unchecked, so a shape failure comes back as the domain's error, or as 404, "
                  + "where 400 belonged.");
    }

    /// <summary>Every concrete FluentValidation validator in the solution.</summary>
    private static IReadOnlyCollection<Type> Validators() =>
        [.. Layers.All
            .SelectMany(layer => layer.AuthoredTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => ValidatedTypeOf(type) is not null)];

    /// <summary>What a request answers with.</summary>
    private static Type? ResponseTypeOf(Type type) =>
        Requests.RequestInterfaceOf(type)?.GetGenericArguments()[0];

    /// <summary>
    /// The type an <c>AbstractValidator&lt;T&gt;</c> validates, or null when the type is not a
    /// validator. Walks the base chain, so a validator deriving from an intermediate base is
    /// still matched to what it ultimately validates.
    /// </summary>
    private static Type? ValidatedTypeOf(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
