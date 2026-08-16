using System.Text.RegularExpressions;
using Mediator;

namespace Todo.ArchitectureTests;

/// <summary>
/// The shape every vertical slice repeats: what is public, where a file goes, and what an
/// endpoint derives from.
/// </summary>
/// <remarks>
/// None of this is enforced by the compiler, and all of it was previously stated only in the
/// <c>new-feature</c> skill — which binds an agent that read the skill and nobody else. A
/// convention that only holds while everyone remembers it is a convention that decays one slice
/// at a time, and the decay is invisible until two slices no longer look alike.
/// </remarks>
public sealed partial class SliceStructureTests
{
    /// <summary>Rule: <see cref="Rules.RequestsArePublicAndHandlersAreInternal"/>.</summary>
    [Fact]
    public void Requests_AndTheirHandlers_HaveTheExpectedAccessibility() =>
        Rule.OverTypes(
            Rules.RequestsArePublicAndHandlersAreInternal,
            [.. Requests.All(), .. Requests.Handlers()],
            type => Requests.IsHandler(type)
                ? type.IsPublic
                    ? "is a handler and is public. Nothing outside Todo.Application constructs a "
                      + "handler; Mediator resolves it. Make it internal."
                    : null
                : type.IsPublic
                    ? null
                    : "is a request record and is not public. The integration tests construct it "
                      + "to post it, so it has to be reachable from the test assembly.");

    /// <summary>
    /// Rule: <see cref="Rules.RequestsLiveInAUseCaseFolder"/>. Namespaces follow folders here, so
    /// asserting on the namespace is asserting on the directory the file sits in.
    /// </summary>
    [Fact]
    public void Requests_EveryOne_LivesInItsOwnUseCaseFolder() =>
        Rule.OverTypes(
            Rules.RequestsLiveInAUseCaseFolder,
            Requests.All(),
            type => UseCaseFolder().IsMatch(type.Namespace ?? string.Empty)
                ? null
                : $"lives in '{type.Namespace}'. A request belongs in "
                  + "<Feature>/Commands/<UseCase>/ or <Feature>/Queries/<UseCase>/ — one folder "
                  + "per use case, not loose in Commands/.");

    /// <summary>Rule: <see cref="Rules.DtosLiveInTheirFeaturesDtosNamespace"/>.</summary>
    [Fact]
    public void Dtos_EveryOne_LivesInItsFeaturesDtosNamespace() =>
        Rule.OverTypes(
            Rules.DtosLiveInTheirFeaturesDtosNamespace,
            Dtos(),
            type => type.Namespace?.EndsWith(".Dtos", StringComparison.Ordinal) == true
                ? null
                : $"lives in '{type.Namespace}'. DTOs are shared between use cases, so they belong "
                  + "to the feature's Dtos folder rather than to whichever use case declared one "
                  + "first.");

    /// <summary>
    /// Rule: <see cref="Rules.NothingLivesAtAFeatureRoot"/>. Quantified over features rather than
    /// over types, because the claim is about every feature and there is currently one — counting
    /// its twenty-odd types would dress a single example up as a population.
    /// </summary>
    [Fact]
    public void Features_HaveNothingLooseAtTheirRoot() =>
        Rule.Over(
            Rules.NothingLivesAtAFeatureRoot,
            ApplicationFeatures(),
            feature => feature.Loose.Count == 0
                ? null
                : $"holds {string.Join(", ", feature.Loose)} directly. Every folder in a feature "
                  + "says what it holds - Abstractions/, Commands/, Dtos/, Events/, Queries/ - and "
                  + "a file beside them says nothing until it is opened.",
            feature => feature.Namespace);

    /// <summary>
    /// Rule: <see cref="Rules.EndpointsDeriveFromTheirFeatureBase"/>. Implementing
    /// <c>IEndpoint</c> directly compiles and routes, and quietly restates the prefix and tag that
    /// the feature's base exists to state once.
    /// </summary>
    [Fact]
    public void Endpoints_EveryOne_DerivesFromItsFeatureBase() =>
        Rule.OverTypes(
            Rules.EndpointsDeriveFromTheirFeatureBase,
            ConcreteEndpoints(),
            type => type.BaseType is { IsAbstract: true } baseType && IsEndpoint(baseType)
                ? null
                : "implements IEndpoint directly instead of deriving from its feature's abstract "
                  + "endpoint base, so its route prefix and OpenAPI tag are declared here rather "
                  + "than once for the whole feature.");

    /// <summary>
    /// <c>Todo.Application.&lt;Feature&gt;.Commands|Queries.&lt;UseCase&gt;</c> — exactly one
    /// segment for the feature and one for the use case, so a request one level too shallow or
    /// too deep is caught.
    /// </summary>
    [GeneratedRegex(@"^Todo\.Application\.[^.]+\.(Commands|Queries)\.[^.]+$", RegexOptions.CultureInvariant)]
    private static partial Regex UseCaseFolder();

    /// <summary>
    /// Every feature in Todo.Application, carrying whatever sits directly at its root.
    /// </summary>
    /// <remarks>
    /// <c>Common</c> is not a feature - it is the layer's shared folder, and its own subfolders
    /// (<c>Behaviours/</c>, <c>Persistence/</c>) already say what they hold. Types in the assembly
    /// root, such as <c>ConfigureServices</c>, are in no feature at all and are not examined.
    /// </remarks>
    private static IReadOnlyCollection<ApplicationFeature> ApplicationFeatures() =>
        [.. Layers.Application.AuthoredTypes
            .Where(type => !type.IsNested)
            .Select(type => (Type: type, Namespace: type.Namespace ?? string.Empty))
            .Where(candidate => candidate.Namespace.StartsWith(FeaturePrefix, StringComparison.Ordinal))
            .Select(candidate => (candidate.Type, candidate.Namespace, Feature: FeatureOf(candidate.Namespace)))
            .Where(candidate => !string.Equals(candidate.Feature, "Common", StringComparison.Ordinal))
            .GroupBy(candidate => candidate.Feature, StringComparer.Ordinal)
            .Select(feature => new ApplicationFeature(
                FeaturePrefix + feature.Key,
                [.. feature
                    .Where(candidate => string.Equals(
                        candidate.Namespace,
                        FeaturePrefix + feature.Key,
                        StringComparison.Ordinal))
                    .Select(candidate => candidate.Type.Name)
                    .Order(StringComparer.Ordinal)]))];

    /// <summary>The first namespace segment below this is the feature name.</summary>
    private const string FeaturePrefix = "Todo.Application.";

    private static string FeatureOf(string @namespace)
    {
        var below = @namespace[FeaturePrefix.Length..];
        var nextDot = below.IndexOf('.');

        return nextDot < 0 ? below : below[..nextDot];
    }

    /// <summary>Types named as DTOs. The name is the claim; where they live is the rule.</summary>
    private static IReadOnlyCollection<Type> Dtos() =>
        [.. Layers.Application.AuthoredTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsNested: false })
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))];

    private static IReadOnlyCollection<Type> ConcreteEndpoints() =>
        [.. Layers.Api.AuthoredTypes
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(IsEndpoint)];

    private static bool IsEndpoint(Type type) =>
        Array.Exists(type.GetInterfaces(), contract => contract.Name == "IEndpoint");
}

/// <summary>
/// A feature in Todo.Application and the types sitting directly at its root, which should be none.
/// </summary>
/// <param name="Namespace">The feature's root namespace, which is also its folder.</param>
/// <param name="Loose">Names of the types in that namespace itself rather than below it.</param>
internal sealed record ApplicationFeature(string Namespace, IReadOnlyList<string> Loose);

/// <summary>
/// Mediator requests and their handlers, selected once and shared by the rules that examine them.
/// </summary>
internal static class Requests
{
    public static IReadOnlyCollection<Type> All() =>
        [.. Authored().Where(type => RequestInterfaceOf(type) is not null)];

    public static IReadOnlyCollection<Type> Handlers() =>
        [.. Authored().Where(IsHandler)];

    public static bool IsHandler(Type type) =>
        Array.Exists(
            type.GetInterfaces(),
            contract => contract.IsGenericType
                && (contract.GetGenericTypeDefinition() == typeof(IRequestHandler<>)
                    || contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

    public static Type? RequestInterfaceOf(Type type) =>
        Array.Find(
            type.GetInterfaces(),
            contract => contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IRequest<>));

    private static IEnumerable<Type> Authored() =>
        Layers.All
            .SelectMany(layer => layer.AuthoredTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false });
}
