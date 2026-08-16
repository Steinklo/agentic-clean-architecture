using System.Text.RegularExpressions;

namespace Todo.ArchitectureTests;

/// <summary>
/// Namespaces follow folders, across every authored source file in <c>src/</c>.
/// </summary>
/// <remarks>
/// This one underwrites another. <see cref="Rules.RequestsLiveInAUseCaseFolder"/> asserts on a
/// namespace and claims to be asserting on a directory, and that claim is only true while these
/// two agree. A single file whose namespace no longer matches its folder makes the folder rule
/// quietly weaker without breaking it, which is the kind of erosion nothing else would report.
/// <para>
/// <b>Why the source and not reflection.</b> A compiled type knows its namespace and has no idea
/// which file it came from, so the pairing this rule is about does not survive compilation.
/// <c>ErrorCodeUniquenessTests</c> reads the domain's sources for the same reason.
/// </para>
/// </remarks>
public sealed partial class SourceLayoutTests
{
    /// <summary>Rule: <see cref="Rules.NamespacesFollowFolders"/>.</summary>
    [Fact]
    public void SourceFiles_EveryOne_DeclaresTheNamespaceItsFolderImplies() =>
        Rule.Over(
            Rules.NamespacesFollowFolders,
            NamespacedSourceFiles(),
            file => file.Declared == file.Expected
                ? null
                : $"declares namespace '{file.Declared}' but its folder implies "
                  + $"'{file.Expected}'. Moving a file changes its namespace and every using that "
                  + "named it; leaving the two disagreeing weakens every rule that reads a "
                  + "namespace as a folder.",
            file => file.RelativePath);

    /// <summary>
    /// Every <c>.cs</c> file under <c>src/</c> that declares a namespace, paired with the one its
    /// folder implies. Files with no namespace — <c>Program.cs</c> under top-level statements — are
    /// not violations and are skipped. Generated migrations are included deliberately: EF writes
    /// them into a folder and names them after it, so they should agree like anything else.
    /// </summary>
    private static List<(string RelativePath, string Declared, string Expected)>
        NamespacedSourceFiles()
    {
        var files = new List<(string RelativePath, string Declared, string Expected)>();

        foreach (var layer in Layers.All)
        {
            var projectDirectory = Path.GetDirectoryName(layer.ProjectFilePath)!;

            foreach (var path in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(path, projectDirectory))
                {
                    continue;
                }

                var declared = FileScopedNamespace().Match(File.ReadAllText(path));

                if (!declared.Success)
                {
                    continue;
                }

                var folder = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, path))!;

                var expected = folder.Length == 0
                    ? layer.ProjectName
                    : $"{layer.ProjectName}.{folder.Replace(Path.DirectorySeparatorChar, '.')}";

                files.Add((
                    Path.GetRelativePath(projectDirectory, path).Replace(Path.DirectorySeparatorChar, '/'),
                    declared.Groups["ns"].Value,
                    expected));
            }
        }

        return files;
    }

    private static bool IsBuildOutput(string path, string projectDirectory) =>
        Path.GetRelativePath(projectDirectory, path)
            .Split(Path.DirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");

    /// <summary>
    /// File-scoped namespaces only, which is the convention here and is enforced by the editor
    /// configuration. A block-scoped namespace would not match and would read as "no namespace",
    /// so it is worth knowing this rule assumes the style rather than checking it.
    /// </summary>
    [GeneratedRegex(@"^namespace\s+(?<ns>[A-Za-z0-9_.]+)\s*;", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FileScopedNamespace();
}
