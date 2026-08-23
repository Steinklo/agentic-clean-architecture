using System.Xml.Linq;

namespace Todo.ArchitectureTests;

/// <summary>
/// Reads the <c>PackageReference</c> graph out of a project file.
/// </summary>
/// <remarks>
/// Mirrors <see cref="ProjectReferences"/> for the same reason: reading the <c>.csproj</c>
/// catches a package that is declared but not yet used, which a compiled-assembly check cannot.
/// Central package management means every reference here carries no <c>Version</c> attribute -
/// that lives in <c>Directory.Packages.props</c> - so only the package name is read.
/// </remarks>
internal static class PackageReferences
{
    /// <summary>The packages <paramref name="layer"/> references, by package name, sorted.</summary>
    public static IReadOnlyList<string> Of(Layer layer)
    {
        if (!File.Exists(layer.ProjectFilePath))
        {
            throw new FileNotFoundException(
                $"Cannot check the package references of {layer.ProjectName}: no project file at this path.",
                layer.ProjectFilePath);
        }

        return In(File.ReadAllText(layer.ProjectFilePath));
    }

    /// <summary>
    /// The packages referenced by the given project file contents, by package name, sorted.
    /// Split out from <see cref="Of"/> so the reader itself can be tested against known input.
    /// </summary>
    public static IReadOnlyList<string> In(string projectFileContents) =>
        [.. XDocument
            .Parse(projectFileContents)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .OrderBy(package => package, StringComparer.Ordinal)];
}
