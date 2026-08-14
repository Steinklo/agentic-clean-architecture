namespace Todo.ArchitectureTests;

/// <summary>
/// Handler naming. A handler that is not called <c>*Handler</c> is invisible to anyone scanning
/// the tree for the code that answers a request.
/// </summary>
public sealed class HandlerNamingTests
{
    /// <summary>
    /// Rule: <see cref="Rules.RequestHandlersAreNamedHandler"/>.
    /// Live since ticket 05 added the first request handlers. See <see cref="RuleCoverage"/> for
    /// why coverage is declared rather than left to look like a passing rule.
    /// </summary>
    [Fact]
    public void RequestHandlers_EveryImplementation_IsNamedWithHandlerSuffix() =>
        Rule.OverTypes(
            Rules.RequestHandlersAreNamedHandler,
            Requests.Handlers(),
            type => type.Name.EndsWith("Handler", StringComparison.Ordinal)
                ? null
                : "implements IRequestHandler but its name does not end in 'Handler'.");
}
