using Mono.Cecil;

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
/// <para>
/// <b>This reads the assembly with Mono.Cecil directly rather than NetArchTest's
/// <c>HaveDependencyOnAny</c>.</b> <c>Types.InAssembly(...).GetTypes()</c> silently drops
/// compiler-generated types — closures, async state machines, the <c>&lt;&gt;c</c> cache behind a
/// non-capturing lambda — and every endpoint in this solution is written as an inline lambda
/// passed to <c>Map*(...)</c>. An EF Core call made from inside one of those lambdas is invisible
/// to NetArchTest, so this rule would report green while the exact thing it exists to catch slipped
/// through in the one place a caller cannot see it. Walking every type Cecil actually finds in the
/// module — nested and compiler-generated included — is what closes that.
/// </para>
/// </remarks>
public sealed class PersistenceIsolationTests
{
    /// <summary>
    /// Namespace prefixes that mean "this type is talking to the object-relational mapper".
    /// Matched by prefix, so this covers EntityFrameworkCore.SqlServer, .Relational, .Design and
    /// the rest.
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

    private static void AssertNoOrmTypes(ArchitectureRule rule, Layer layer)
    {
        using var module = ModuleDefinition.ReadModule(layer.Assembly.Location);

        var types = AllTypes(module.Types).ToList();

        Rule.Over(
            rule,
            types,
            type => ReferencesOrmNamespace(type) ? "references an object-relational mapping type" : null,
            type => type.FullName);
    }

    /// <summary>Every type Cecil finds in the module, nested and compiler-generated included.</summary>
    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types) =>
        types.SelectMany(type => new[] { type }.Concat(AllTypes(type.NestedTypes)));

    /// <summary>
    /// Whether <paramref name="type"/> names an ORM type anywhere a caller could reach — its base
    /// type, its interfaces, a field, a method signature, or a call inside a method body.
    /// </summary>
    private static bool ReferencesOrmNamespace(TypeDefinition type)
    {
        if (IsOrmNamespace(type.BaseType?.Namespace))
        {
            return true;
        }

        if (type.Interfaces.Any(implemented => IsOrmNamespace(implemented.InterfaceType.Namespace)))
        {
            return true;
        }

        if (type.Fields.Any(field => IsOrmNamespace(field.FieldType.Namespace)))
        {
            return true;
        }

        return type.Methods.Any(MethodReferencesOrmNamespace);
    }

    private static bool MethodReferencesOrmNamespace(MethodDefinition method)
    {
        if (IsOrmNamespace(method.ReturnType.Namespace)
            || method.Parameters.Any(parameter => IsOrmNamespace(parameter.ParameterType.Namespace)))
        {
            return true;
        }

        if (!method.HasBody)
        {
            return false;
        }

        return method.Body.Instructions.Any(instruction => OperandReferencesOrmNamespace(instruction.Operand));
    }

    /// <summary>
    /// Whether an instruction's operand names an ORM type. <see cref="TypeReference"/> is checked
    /// first because <see cref="MemberReference"/> is its base — a field, method or property
    /// operand's own type never matters here, only what it points at.
    /// </summary>
    private static bool OperandReferencesOrmNamespace(object? operand) => operand switch
    {
        TypeReference type => IsOrmNamespace(type.Namespace),
        MemberReference member => IsOrmNamespace(member.DeclaringType?.Namespace),
        _ => false
    };

    private static bool IsOrmNamespace(string? @namespace) =>
        @namespace is not null
        && Array.Exists(_ormNamespaces, prefix => @namespace.StartsWith(prefix, StringComparison.Ordinal));
}
