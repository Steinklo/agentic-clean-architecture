namespace Todo.ArchitectureTests;

/// <summary>
/// Object-relational mapping types belong to Infrastructure and nowhere else.
/// </summary>
/// <remarks>
/// This is the rule that makes mapping the domain model directly safe. Because EF Core maps it —
/// no <c>Document</c> layer, no mappers — nothing <em>structurally</em> stops an EF type drifting
/// into Domain or Application. That hole is plugged here rather than by the compiler.
/// <para>
/// Note what is deliberately <em>not</em> forbidden: Api referencing the Infrastructure project.
/// Api composes the application by calling <c>AddInfrastructureServices(...)</c>, an extension
/// method declared inside Infrastructure, so the EF types stay behind that call and Api never
/// names one itself. The rule below forbids the EF namespaces, not the project reference, which is
/// exactly what makes that composition pattern legal and a stray <c>AddDbContext&lt;T&gt;()</c> in
/// <c>Program.cs</c> illegal.
/// </para>
/// </remarks>
public sealed class PersistenceIsolationTests
{
    /// <summary>
    /// Namespace prefixes that mean "this type is talking to the object-relational mapper".
    /// NetArchTest matches a dependency by prefix, so this covers EntityFrameworkCore.SqlServer,
    /// .Relational, .Design and the rest.
    /// </summary>
    private static readonly string[] _ormNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.SqlClient"
    ];

    /// <summary>Rule: <see cref="Rules.DomainUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Domain_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoOrmTypes(Rules.DomainUsesNoOrmTypes, Layers.Domain);

    /// <summary>Rule: <see cref="Rules.ApplicationUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Application_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoOrmTypes(Rules.ApplicationUsesNoOrmTypes, Layers.Application);

    /// <summary>Rule: <see cref="Rules.ApiUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Api_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoOrmTypes(Rules.ApiUsesNoOrmTypes, Layers.Api);

    private static void AssertNoOrmTypes(ArchitectureRule rule, Layer layer) =>
        Rule.OverLayer(
            rule,
            layer,
            types => types.ShouldNot().HaveDependencyOnAny(_ormNamespaces).GetResult());
}
