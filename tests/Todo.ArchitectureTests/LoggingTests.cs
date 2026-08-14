using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Todo.ArchitectureTests;

/// <summary>
/// Logged event ids, which are a test contract here and not just diagnostics.
/// </summary>
/// <remarks>
/// The integration tests prove a domain event was dispatched by matching on its
/// <c>EventId</c> rather than on message text, so that rewording a log line does not break a test.
/// The cost of that choice is that two events sharing an id makes such an assertion pass for the
/// wrong reason — the test goes green while watching the wrong event. Ids are allocated in blocks
/// per feature for the same reason, and the <c>new-feature</c> skill says which block is next.
/// </remarks>
public sealed class LoggingTests
{
    /// <summary>Rule: <see cref="Rules.LoggedEventIdsAreUnique"/>.</summary>
    [Fact]
    public void LoggedEventIds_AcrossTheSolution_AreUnique()
    {
        var byId = LoggedEvents()
            .GroupBy(logged => logged.EventId)
            .ToDictionary(group => group.Key, group => group.Select(logged => logged.Method).ToList());

        Rule.Over(
            Rules.LoggedEventIdsAreUnique,
            LoggedEvents(),
            logged => byId[logged.EventId].Count == 1
                ? null
                : $"shares EventId {logged.EventId} with "
                  + $"{string.Join(", ", byId[logged.EventId].Where(other => other != logged.Method))}. "
                  + "An integration test matching on that id cannot tell the two apart, so it can "
                  + "pass while watching the wrong event. Give this one the next free id in its "
                  + "feature's block.",
            logged => logged.Method);
    }

    /// <summary>
    /// Every source-generated log method in the solution, with the id it declares. Read from the
    /// attribute rather than from source, so a method whose id is written in any legal form is
    /// still seen.
    /// </summary>
    private static IReadOnlyCollection<(string Method, int EventId)> LoggedEvents() =>
        [.. Layers.All
            .SelectMany(layer => layer.AuthoredTypes)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => (Method: method, Attribute: method.GetCustomAttribute<LoggerMessageAttribute>()))
            .Where(candidate => candidate.Attribute is not null)
            .Select(candidate => (
                Method: $"{candidate.Method.DeclaringType!.Name}.{candidate.Method.Name}",
                EventId: candidate.Attribute!.EventId))];
}
