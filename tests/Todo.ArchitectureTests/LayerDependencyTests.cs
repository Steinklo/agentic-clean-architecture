namespace Todo.ArchitectureTests;

/// <summary>
/// The project reference graph: which layer is allowed to know that which other layer exists.
/// </summary>
public sealed class LayerDependencyTests
{
    /// <summary>Rule: <see cref="Rules.DomainHasNoProjectReferences"/>.</summary>
    [Fact]
    public void Domain_ProjectReferences_AreNone() =>
        Rule.OverProjectReferences(
            Rules.DomainHasNoProjectReferences,
            Layers.Domain,
            references => references.Count == 0
                ? null
                : "Domain sits at the centre and depends on nothing, but it references: "
                  + string.Join(", ", references));

    /// <summary>Rule: <see cref="Rules.ApplicationReferencesOnlyDomain"/>.</summary>
    [Fact]
    public void Application_ProjectReferences_AreDomainOnly() =>
        Rule.OverProjectReferences(
            Rules.ApplicationReferencesOnlyDomain,
            Layers.Application,
            references => string.Join(", ", references) == Layers.Domain.ProjectName
                ? null
                : $"Application may reference {Layers.Domain.ProjectName} and nothing else, but it references: "
                  + (references.Count == 0 ? "(nothing)" : string.Join(", ", references)));

    /// <summary>
    /// Guards <see cref="Domain_ProjectReferences_AreNone"/>, whose passing state is an empty
    /// result. Without this, a reader — or a reference reader that silently found nothing — could
    /// not tell a clean project file from a broken parser. This proves the reader finds references
    /// when there are references to find.
    /// </summary>
    [Fact]
    public void ProjectReferences_GivenAProjectFileWithReferences_FindsEveryOne()
    {
        const string ProjectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Todo.Application\Todo.Application.csproj" />
                <ProjectReference Include="..\Todo.Domain\Todo.Domain.csproj" />
              </ItemGroup>
            </Project>
            """;

        var references = ProjectReferences.In(ProjectFileContents);

        Assert.Equal("Todo.Application, Todo.Domain", string.Join(", ", references));
    }

    /// <summary>
    /// Rule: <see cref="Rules.DomainReferencesOnlyMediatorAbstractions"/>. Read from
    /// <c>Todo.Domain.csproj</c> rather than the compiled assembly for the same reason as the
    /// project-reference rules: a package pulled in and not yet used would not show up in the
    /// compiled output, and "declared but unused" is exactly the drift worth catching early.
    /// </summary>
    [Fact]
    public void Domain_PackageReferences_AreMediatorAbstractionsOnly() =>
        Rule.OverPackageReferences(
            Rules.DomainReferencesOnlyMediatorAbstractions,
            Layers.Domain,
            references => string.Join(", ", references) == "Mediator.Abstractions"
                ? null
                : "Domain may reference only the Mediator.Abstractions package, but it references: "
                  + (references.Count == 0 ? "(nothing)" : string.Join(", ", references)));

    /// <summary>
    /// Guards <see cref="Domain_PackageReferences_AreMediatorAbstractionsOnly"/> the same way
    /// <see cref="ProjectReferences_GivenAProjectFileWithReferences_FindsEveryOne"/> guards its
    /// project-reference counterpart.
    /// </summary>
    [Fact]
    public void PackageReferences_GivenAProjectFileWithReferences_FindsEveryOne()
    {
        const string ProjectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Mediator.Abstractions" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
              </ItemGroup>
            </Project>
            """;

        var references = PackageReferences.In(ProjectFileContents);

        Assert.Equal(
            "Mediator.Abstractions, Microsoft.EntityFrameworkCore.SqlServer",
            string.Join(", ", references));
    }
}
