using NetArchTest.Rules;

namespace Todo.ArchitectureTests;

/// <summary>
/// Runs an <see cref="ArchitectureRule"/> and reports it.
/// </summary>
/// <remarks>
/// Every entry point here does two things in order. First it checks the rule's declared
/// <see cref="RuleCoverage"/> against the number of items actually examined — this is what stops a
/// rule passing because there was nothing to test. Only then does it check the rule itself.
/// Failure messages lead with the rule's sentence, so the test output names the broken rule
/// without anyone opening the test body.
/// </remarks>
internal static class Rule
{
    /// <summary>Applies <paramref name="violation"/> to every examined type.</summary>
    /// <param name="rule">The rule being enforced.</param>
    /// <param name="examined">The types the rule applies to.</param>
    /// <param name="violation">Returns why the type breaks the rule, or null if it does not.</param>
    public static void OverTypes(
        ArchitectureRule rule,
        IReadOnlyCollection<Type> examined,
        Func<Type, string?> violation) =>
        Over(rule, examined, violation, type => type.FullName ?? type.Name);

    /// <summary>
    /// Applies <paramref name="violation"/> to every examined item, whatever it is.
    /// </summary>
    /// <remarks>
    /// Most rules read types, but not all: the mapping rules read model metadata, and the layout
    /// rule reads source files. The coverage guard is the reason they share this rather than
    /// asserting for themselves — a rule that examines files can pass vacuously in exactly the way
    /// a rule that examines types can.
    /// </remarks>
    /// <typeparam name="T">What the rule examines.</typeparam>
    /// <param name="rule">The rule being enforced.</param>
    /// <param name="examined">The items the rule applies to.</param>
    /// <param name="violation">Returns why the item breaks the rule, or null if it does not.</param>
    /// <param name="name">How to name an item in the failure message.</param>
    public static void Over<T>(
        ArchitectureRule rule,
        IReadOnlyCollection<T> examined,
        Func<T, string?> violation,
        Func<T, string> name)
    {
        AssertCoverage(rule, examined.Count);

        var violations = examined
            .Select(item => (Name: name(item), Why: violation(item)))
            .Where(candidate => candidate.Why is not null)
            .Select(candidate => $"  - {candidate.Name} {candidate.Why}")
            .ToList();

        if (violations.Count > 0)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE BROKEN - {rule.Name}."
                + Environment.NewLine
                + $"{violations.Count} of the {examined.Count} examined break it:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
        }
    }

    /// <summary>Applies a NetArchTest rule to every type in <paramref name="layer"/>.</summary>
    /// <param name="rule">The rule being enforced.</param>
    /// <param name="layer">The layer whose types are examined.</param>
    /// <param name="assertion">The NetArchTest expression to evaluate.</param>
    public static void OverLayer(ArchitectureRule rule, Layer layer, Func<Types, TestResult> assertion)
    {
        // Counted from NetArchTest's own view of the assembly, so the number reported is exactly
        // the set the assertion below ran against.
        var examinedCount = Types.InAssembly(layer.Assembly).GetTypes().Count();

        AssertCoverage(rule, examinedCount);

        var result = assertion(Types.InAssembly(layer.Assembly));

        if (!result.IsSuccessful)
        {
            var failing = result.FailingTypeNames.ToList();

            Assert.Fail(
                $"ARCHITECTURE RULE BROKEN - {rule.Name}."
                + Environment.NewLine
                + $"{failing.Count} of the {examinedCount} types in {layer.ProjectName} break it:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, failing.Select(name => $"  - {name}")));
        }
    }

    /// <summary>Applies <paramref name="violation"/> to a layer's declared project references.</summary>
    /// <param name="rule">The rule being enforced.</param>
    /// <param name="layer">The layer whose project file is read.</param>
    /// <param name="violation">Returns why the reference set breaks the rule, or null if it does not.</param>
    public static void OverProjectReferences(
        ArchitectureRule rule,
        Layer layer,
        Func<IReadOnlyList<string>, string?> violation)
    {
        // Of() throws if the project file is missing, so this rule cannot pass by examining nothing.
        var why = violation(ProjectReferences.Of(layer));

        if (why is not null)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE BROKEN - {rule.Name}."
                + Environment.NewLine
                + $"  {layer.ProjectFilePath}"
                + Environment.NewLine
                + $"  {why}");
        }
    }

    /// <summary>
    /// The guard against a rule passing because it examined nothing. See <see cref="RuleCoverage"/>.
    /// </summary>
    private static void AssertCoverage(ArchitectureRule rule, int examinedCount)
    {
        if (rule.Coverage == RuleCoverage.Live && examinedCount == 0)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE VACUOUS - {rule.Name}."
                + Environment.NewLine
                + "  It is declared Live in Rules.cs, but it examined 0 types, so it passed without"
                + Environment.NewLine
                + "  testing anything. Either the rule's type selector has broken, or the code it"
                + Environment.NewLine
                + "  guards has been deleted. Fix the selector, or declare the rule Dormant.");
        }

        if (rule.Coverage == RuleCoverage.Live && examinedCount < rule.MeaningfulAt)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE THINNER THAN DECLARED - {rule.Name}."
                + Environment.NewLine
                + $"  It is declared Live in Rules.cs and meaningful at {rule.MeaningfulAt} types, but it"
                + Environment.NewLine
                + $"  examined {examinedCount}. It still passes on what it saw; it just is not the evidence the"
                + Environment.NewLine
                + "  declaration claims. Either the selector has narrowed, or the population it is"
                + Environment.NewLine
                + "  quantified over has shrunk. Fix the selector, or declare the rule Thin.");
        }

        if (rule.Coverage == RuleCoverage.Thin && examinedCount == 0)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE VACUOUS - {rule.Name}."
                + Environment.NewLine
                + "  It is declared Thin in Rules.cs, which means it examines a little. It examined 0,"
                + Environment.NewLine
                + "  so it proved nothing at all. Fix the selector, or declare the rule Dormant.");
        }

        if (rule.Coverage == RuleCoverage.Thin && examinedCount >= rule.MeaningfulAt)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE NO LONGER THIN - {rule.Name}."
                + Environment.NewLine
                + $"  It is declared Thin in Rules.cs - too narrow to be evidence - but it now examines"
                + Environment.NewLine
                + $"  {examinedCount} type(s), at or past the {rule.MeaningfulAt} it declared itself meaningful at."
                + Environment.NewLine
                + "  Change its coverage to RuleCoverage.Live. Do not raise MeaningfulAt to keep it Thin;"
                + Environment.NewLine
                + "  that is the ratchet running backwards.");
        }

        if (rule.Coverage == RuleCoverage.Dormant && examinedCount > 0)
        {
            Assert.Fail(
                $"ARCHITECTURE RULE NO LONGER DORMANT - {rule.Name}."
                + Environment.NewLine
                + "  It is declared Dormant in Rules.cs - meaning it had nothing to examine - but it"
                + Environment.NewLine
                + $"  now examines {examinedCount} type(s). Change its coverage to RuleCoverage.Live, or to"
                + Environment.NewLine
                + "  RuleCoverage.Thin if that is still too few to be evidence, so the suite stops"
                + Environment.NewLine
                + "  advertising an untested rule as a passing one.");
        }
    }
}
