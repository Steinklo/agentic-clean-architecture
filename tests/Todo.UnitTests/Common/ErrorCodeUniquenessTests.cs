using System.Text.RegularExpressions;

namespace Todo.UnitTests.Common;

/// <summary>
/// Every error code the domain raises is distinct.
/// </summary>
/// <remarks>
/// <para>
/// Error codes used to live in one static <c>TodoListErrors</c> class, and a duplicate was
/// impossible there because two members cannot share a name. Now that each error is written inline
/// at the guard that raises it, nothing structural stops two guards from shipping the same code for
/// two different rules - which is the one failure mode inlining introduces, because a code is the
/// contract the integration tests and <c>docs/api.md</c> assert on.
/// </para>
/// <para>
/// <b>Why the source and not reflection.</b> Reflection was the first idea and it cannot work: an
/// inlined error is constructed inside a method body, so there is no member on any type to
/// enumerate. Reading the codes back would mean invoking every guard with inputs that make it fail,
/// which is the thing that cannot be done generically. So this reads the domain's <c>.cs</c> files
/// and finds every <c>DomainError.Validation/NotFound/Conflict/Failure/NotImplemented</c> call whose
/// first argument is a literal, which is exactly how every error in Todo.Domain is written.
/// </para>
/// <para>
/// <b>The alternation above is a second habit, beside the one in <c>ResultExtensions</c>.</b> A
/// category added to <c>DomainError</c> without being added to that pattern is not a compile error;
/// its codes simply stop being scanned, and this test keeps passing while covering less.
/// <c>NotImplemented</c> was added to it when the category arrived, and adds no coverage today —
/// the scan reads Todo.Domain, and that category belongs to adapters rather than to guards. It is
/// there so the pattern stays a list of every category rather than of the ones that happened to
/// matter when it was written.
/// </para>
/// <para>
/// <b>How it fails.</b> Two occurrences of the same literal - the same code raised at two guards -
/// make the assertion fail and name both file:line locations. Seeding a duplicate is the way to
/// check that for yourself; doing so was how this test was verified.
/// </para>
/// <para>
/// <b>The limit, stated honestly.</b> A code assembled at runtime rather than written as a literal
/// is invisible here. That is not a gap worth closing: the domain writes every code as a literal on
/// purpose, and a computed error code would be a defect in its own right. The scan also guards
/// itself - finding no source files, or no error codes at all, fails rather than passing on an
/// empty set, so a moved folder or a broken pattern cannot make this test quietly vacuous.
/// </para>
/// </remarks>
public partial class ErrorCodeUniquenessTests
{
    [Fact]
    public void ErrorCodes_AcrossTheDomain_AreUnique()
    {
        var sources = DomainSourceFiles();

        Assert.NotEmpty(sources);

        var declarations = sources.SelectMany(DeclaredErrorCodes).ToList();

        Assert.NotEmpty(declarations);

        var duplicates = declarations
            .GroupBy(declaration => declaration.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"  - '{group.Key}' is raised at {group.Count()} guards:"
                + Environment.NewLine
                + string.Join(
                    Environment.NewLine,
                    group.Select(declaration => $"      {declaration.Location}")))
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            "DUPLICATE DOMAIN ERROR CODE."
            + Environment.NewLine
            + "An error code is the contract callers assert on, so one code must mean one rule."
            + Environment.NewLine
            + $"{duplicates.Count} code(s) are raised from more than one place:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, duplicates));
    }

    /// <summary>
    /// Matches a call to one of <see cref="Todo.Domain.Common.DomainError"/>'s category factories whose
    /// first argument is a string literal - the code. The literal may sit on the following line, so
    /// whitespace between the parenthesis and the quote is allowed.
    /// </summary>
    [GeneratedRegex(
        """DomainError\.(?:Validation|NotFound|Conflict|Failure|NotImplemented)\s*\(\s*"(?<code>[^"]+)"\s*,""",
        RegexOptions.None,
        matchTimeoutMilliseconds: 5000)]
    private static partial Regex ErrorFactoryCall();

    private static IReadOnlyList<(string Code, string Location)> DeclaredErrorCodes(string path)
    {
        var text = File.ReadAllText(path);

        return
        [
            .. ErrorFactoryCall()
                .Matches(text)
                .Select(match => (
                    Code: match.Groups["code"].Value,
                    Location: $"{Path.GetFileName(path)}:{LineOf(text, match.Index)}"))
        ];
    }

    private static int LineOf(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;

    /// <summary>
    /// Every hand-written <c>.cs</c> file in Todo.Domain - build output excluded, so the scan sees
    /// the source a person edits and nothing else. The solution root is found by walking up from the
    /// test binaries to <c>Todo.slnx</c>, the same way the architecture tests locate project files.
    /// </summary>
    private static IReadOnlyList<string> DomainSourceFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Todo.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new InvalidOperationException(
                $"Could not find Todo.slnx in any directory above '{AppContext.BaseDirectory}'. "
                + "This test reads Todo.Domain's source from disk and cannot run without it.");

        var domain = Path.Combine(root, "src", "Todo.Domain");

        return
        [
            .. Directory
                .EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(Path.GetRelativePath(domain, path)))
        ];
    }

    private static bool IsBuildOutput(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Contains("obj", StringComparer.Ordinal)
            || segments.Contains("bin", StringComparer.Ordinal);
    }
}
