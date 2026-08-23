using System.Reflection;
using System.Runtime.CompilerServices;
using Todo.Domain.Common;

namespace Todo.ArchitectureTests;

/// <summary>
/// The shape of the domain model itself: encapsulation of entities, and the base type every value
/// object is required to share.
/// </summary>
public sealed class DomainModelTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    /// <summary>Rule: <see cref="Rules.EntitiesHaveNoPublicSetters"/>.</summary>
    [Fact]
    public void Entities_DeclaredProperties_HaveNoPublicSetters() =>
        Rule.OverTypes(Rules.EntitiesHaveNoPublicSetters, Entities(), PublicSetterViolation);

    /// <summary>
    /// Rule: <see cref="Rules.AggregateRootsAreKeyedOnGuid"/>. A silent one:
    /// <c>UnitOfWork</c> dispatches domain events by enumerating
    /// <c>ChangeTracker.Entries&lt;AggregateRoot&lt;Guid&gt;&gt;()</c>, which is a closed generic.
    /// A root keyed on anything else compiles, maps, saves its state correctly, and simply never
    /// has an event dispatched — with no error anywhere to say so.
    /// </summary>
    [Fact]
    public void AggregateRoots_EveryOne_IsKeyedOnGuid() =>
        Rule.OverTypes(
            Rules.AggregateRootsAreKeyedOnGuid,
            AggregateRoots(),
            type => KeyTypeOf(type) == typeof(Guid)
                ? null
                : $"is an aggregate root keyed on {KeyTypeOf(type)?.Name ?? "an unknown type"}, not Guid. "
                  + "UnitOfWork enumerates ChangeTracker.Entries<AggregateRoot<Guid>>(), so this "
                  + "root's domain events are raised and never dispatched.");

    /// <summary>Rule: <see cref="Rules.ValueObjectsDeriveFromValueObject"/>.</summary>
    [Fact]
    public void ValueObjects_EveryTypeInAValueObjectsNamespace_DerivesFromValueObject() =>
        Rule.OverTypes(
            Rules.ValueObjectsDeriveFromValueObject,
            ValueObjectCandidates(),
            type => typeof(ValueObject).IsAssignableFrom(type)
                ? null
                : $"lives in a ValueObjects namespace but does not derive from {typeof(ValueObject).FullName}, "
                  + "so it compares by reference instead of by value.");

    /// <summary>
    /// Rule: <see cref="Rules.DomainHasNoInternalsVisibleTo"/>. The aggregate seam tests
    /// (<c>tests/Todo.UnitTests</c>) construct an aggregate and read its <c>Result</c>, never an
    /// internal member directly — an <c>InternalsVisibleTo</c> would mean that stopped being true
    /// somewhere without anyone deciding it should.
    /// </summary>
    [Fact]
    public void Domain_Assembly_GrantsNoInternalsVisibleToAccess() =>
        Rule.Over(
            Rules.DomainHasNoInternalsVisibleTo,
            [Layers.Domain],
            layer =>
            {
                var grants = layer.Assembly
                    .GetCustomAttributes<InternalsVisibleToAttribute>()
                    .Select(attribute => attribute.AssemblyName)
                    .ToList();

                return grants.Count == 0
                    ? null
                    : $"grants InternalsVisibleTo to: {string.Join(", ", grants)}. Domain's internal "
                      + "members are reached only through an aggregate root's public surface; a grant "
                      + "here means something outside the assembly stopped doing that.";
            },
            layer => layer.ProjectName);

    private static string? PublicSetterViolation(Type entity)
    {
        // DeclaredOnly: an inherited setter is reported against the type that declares it, once.
        var settable = entity
            .GetProperties(DeclaredPublicInstance)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToList();

        return settable.Count == 0
            ? null
            : "is an entity, so its state changes only through its own methods, but these properties "
              + $"have a public setter: {string.Join(", ", settable)}.";
    }

    /// <summary>
    /// Every entity in the domain, including the <c>Entity</c> and <c>AggregateRoot</c> bases —
    /// a public setter on <c>Id</c> would break encapsulation for every entity at once.
    /// </summary>
    private static IReadOnlyCollection<Type> Entities() =>
        [.. Layers.Domain.AuthoredTypes.Where(IsEntity)];

    /// <summary>
    /// Every concrete aggregate root. The abstract <c>AggregateRoot&lt;TId&gt;</c> base is excluded:
    /// it is the thing being closed, not an instance of the rule.
    /// </summary>
    private static IReadOnlyCollection<Type> AggregateRoots() =>
        [.. Layers.Domain.AuthoredTypes
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => KeyTypeOf(type) is not null)];

    /// <summary>The <c>TId</c> a type closes <c>AggregateRoot&lt;TId&gt;</c> with, or null.</summary>
    private static Type? KeyTypeOf(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static bool IsEntity(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Types that claim to be value objects by where they live. The namespace is what identifies
    /// them: deriving from the base is the rule under test, so it cannot also be the selector.
    /// </summary>
    private static IReadOnlyCollection<Type> ValueObjectCandidates() =>
        [.. Layers.Domain.AuthoredTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsNested: false })
            .Where(type => type.Namespace?.EndsWith(".ValueObjects", StringComparison.Ordinal) == true)];
}
