using System.Xml.Linq;

namespace Todo.ArchitectureTests;

/// <summary>
/// Reads the <c>ProjectReference</c> graph out of a project file.
/// </summary>
/// <remarks>
/// The reference-graph rules deliberately read <c>.csproj</c> rather than the compiled assembly.
/// The C# compiler elides a reference that is declared but never used, so an assembly manifest
/// cannot tell "Domain references nothing" from "Domain references Infrastructure but has not got
/// round to using it yet" — and the second one is the violation we care about catching early.
/// </remarks>
internal static class ProjectReferences
{
    /// <summary>The projects <paramref name="layer"/> references, by project name, sorted.</summary>
    public static IReadOnlyList<string> Of(Layer layer)
    {
        if (!File.Exists(layer.ProjectFilePath))
        {
            throw new FileNotFoundException(
                $"Cannot check the references of {layer.ProjectName}: no project file at this path.",
                layer.ProjectFilePath);
        }

        return In(File.ReadAllText(layer.ProjectFilePath));
    }

    /// <summary>
    /// The projects referenced by the given project file contents, by project name, sorted.
    /// Split out from <see cref="Of"/> so the reader itself can be tested against known input.
    /// </summary>
    public static IReadOnlyList<string> In(string projectFileContents) =>
        [.. XDocument
            .Parse(projectFileContents)
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
            .OrderBy(projectName => projectName, StringComparer.Ordinal)];
}
