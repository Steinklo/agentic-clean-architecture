using Mono.Cecil;

namespace Todo.ArchitectureTests;

/// <summary>
/// Object-relational mapping types belong to Infrastructure and nowhere else, and the domain
/// model carries no attribute that would teach it something about how it is stored or displayed.
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

    /// <summary>
    /// Namespace prefixes that mean "this type has an opinion about how it is stored or
    /// displayed" — the data-annotation family EF and ASP.NET both understand as mapping or
    /// validation metadata.
    /// </summary>
    private static readonly string[] _persistenceAttributeNamespaces =
    [
        "System.ComponentModel.DataAnnotations"
    ];

    /// <summary>Rule: <see cref="Rules.DomainUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Domain_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoDependencyOn(
            Rules.DomainUsesNoOrmTypes,
            Layers.Domain,
            _ormNamespaces,
            "references an object-relational mapping type");

    /// <summary>Rule: <see cref="Rules.ApplicationUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Application_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoDependencyOn(
            Rules.ApplicationUsesNoOrmTypes,
            Layers.Application,
            _ormNamespaces,
            "references an object-relational mapping type");

    /// <summary>Rule: <see cref="Rules.ApiUsesNoOrmTypes"/>.</summary>
    [Fact]
    public void Api_ObjectRelationalMappingTypes_AreAbsent() =>
        AssertNoDependencyOn(
            Rules.ApiUsesNoOrmTypes,
            Layers.Api,
            _ormNamespaces,
            "references an object-relational mapping type");

    /// <summary>
    /// Rule: <see cref="Rules.DomainUsesNoPersistenceAttributes"/>. A data annotation is the same
    /// mistake as an EF type in Domain, one layer removed: it still teaches the model something
    /// about how it is persisted or displayed, which the mapping in Infrastructure exists so the
    /// model never has to know.
    /// </summary>
    [Fact]
    public void Domain_PersistenceOrDisplayAttributes_AreAbsent() =>
        AssertNoDependencyOn(
            Rules.DomainUsesNoPersistenceAttributes,
            Layers.Domain,
            _persistenceAttributeNamespaces,
            "carries a persistence or display attribute");

    private static void AssertNoDependencyOn(
        ArchitectureRule rule,
        Layer layer,
        string[] namespaces,
        string violationDescription)
    {
        using var module = ModuleDefinition.ReadModule(layer.Assembly.Location);

        var types = AllTypes(module.Types).ToList();

        Rule.Over(
            rule,
            types,
            type => ReferencesNamespace(type, namespaces) ? violationDescription : null,
            type => type.FullName);
    }

    /// <summary>Every type Cecil finds in the module, nested and compiler-generated included.</summary>
    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types) =>
        types.SelectMany(type => new[] { type }.Concat(AllTypes(type.NestedTypes)));

    /// <summary>
    /// Whether <paramref name="type"/> names a type from <paramref name="namespaces"/> anywhere a
    /// caller could reach — its own attributes, its base type, its interfaces, a field, a
    /// property, a method signature, a method's attributes, or a call inside a method body.
    /// </summary>
    private static bool ReferencesNamespace(TypeDefinition type, string[] namespaces)
    {
        if (HasAttributeFrom(type.CustomAttributes, namespaces))
        {
            return true;
        }

        if (IsInNamespace(type.BaseType?.Namespace, namespaces))
        {
            return true;
        }

        if (type.Interfaces.Any(implemented => IsInNamespace(implemented.InterfaceType.Namespace, namespaces)))
        {
            return true;
        }

        if (type.Fields.Any(field =>
                IsInNamespace(field.FieldType.Namespace, namespaces)
                || HasAttributeFrom(field.CustomAttributes, namespaces)))
        {
            return true;
        }

        if (type.Properties.Any(property =>
                IsInNamespace(property.PropertyType.Namespace, namespaces)
                || HasAttributeFrom(property.CustomAttributes, namespaces)))
        {
            return true;
        }

        return type.Methods.Any(method => MethodReferencesNamespace(method, namespaces));
    }

    private static bool MethodReferencesNamespace(MethodDefinition method, string[] namespaces)
    {
        if (HasAttributeFrom(method.CustomAttributes, namespaces)
            || IsInNamespace(method.ReturnType.Namespace, namespaces)
            || method.Parameters.Any(parameter => IsInNamespace(parameter.ParameterType.Namespace, namespaces)))
        {
            return true;
        }

        if (!method.HasBody)
        {
            return false;
        }

        return method.Body.Instructions.Any(
            instruction => OperandReferencesNamespace(instruction.Operand, namespaces));
    }

    /// <summary>
    /// Whether an instruction's operand names a type from <paramref name="namespaces"/>.
    /// <see cref="TypeReference"/> is checked first because <see cref="MemberReference"/> is its
    /// base — a field, method or property operand's own type never matters here, only what it
    /// points at.
    /// </summary>
    private static bool OperandReferencesNamespace(object? operand, string[] namespaces) => operand switch
    {
        TypeReference type => IsInNamespace(type.Namespace, namespaces),
        MemberReference member => IsInNamespace(member.DeclaringType?.Namespace, namespaces),
        _ => false
    };

    private static bool HasAttributeFrom(
        IEnumerable<CustomAttribute> attributes,
        string[] namespaces) =>
        attributes.Any(attribute => IsInNamespace(attribute.AttributeType.Namespace, namespaces));

    private static bool IsInNamespace(string? @namespace, string[] namespaces) =>
        @namespace is not null
        && Array.Exists(namespaces, prefix => @namespace.StartsWith(prefix, StringComparison.Ordinal));
}
