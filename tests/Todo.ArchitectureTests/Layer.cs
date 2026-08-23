using System.Reflection;
using System.Runtime.CompilerServices;

namespace Todo.ArchitectureTests;

/// <summary>
/// One project in the layered architecture, paired with the two things the rules need in order to
/// inspect it: the compiled assembly (for type and dependency rules) and the project file (for
/// reference-graph rules).
/// </summary>
/// <param name="ProjectName">The assembly and project name, e.g. <c>Todo.Domain</c>.</param>
/// <param name="Assembly">The compiled assembly.</param>
/// <param name="ProjectFilePath">Absolute path to the layer's <c>.csproj</c>.</param>
internal sealed record Layer(string ProjectName, Assembly Assembly, string ProjectFilePath)
{
    /// <summary>
    /// The types a person actually wrote. Compiler-generated scaffolding — lambda closures,
    /// iterator state machines, anonymous types — is filtered out, because a rule that reports
    /// <c>&lt;&gt;c__DisplayClass0_0</c> as a violation teaches the reader nothing.
    /// </summary>
    public IReadOnlyList<Type> AuthoredTypes =>
        [.. Assembly
            .GetTypes()
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))];
}

/// <summary>
/// The four layers, resolved once. Assemblies are loaded by name — every layer is a
/// <c>ProjectReference</c> of this test project, so every one of them is in the output folder
/// even while it is still empty.
/// </summary>
internal static class Layers
{
    private static string? _solutionRoot;

    public static Layer Domain { get; } = For("Todo.Domain");

    public static Layer Application { get; } = For("Todo.Application");

    public static Layer Infrastructure { get; } = For("Todo.Infrastructure");

    public static Layer Api { get; } = For("Todo.Api");

    /// <summary>Every layer, innermost first.</summary>
    public static IReadOnlyList<Layer> All { get; } = [Domain, Application, Infrastructure, Api];

    /// <summary>The directory containing <c>Todo.slnx</c>, resolved once and cached.</summary>
    public static string SolutionRoot => _solutionRoot ??= FindSolutionRoot();

    private static Layer For(string projectName) =>
        new(
            projectName,
            Assembly.Load(projectName),
            Path.Combine(SolutionRoot, "src", projectName, $"{projectName}.csproj"));

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Todo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                $"Could not find Todo.slnx in any directory above '{AppContext.BaseDirectory}'. "
                + "The project-reference rules read the .csproj files from disk and cannot run without it.");
    }
}
