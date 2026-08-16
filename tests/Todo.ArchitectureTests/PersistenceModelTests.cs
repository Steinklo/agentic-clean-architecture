using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Todo.Domain.Common;
using Todo.Infrastructure.Persistence;

namespace Todo.ArchitectureTests;

/// <summary>
/// The Entity Framework model, asserted against the mapping gotchas <c>AGENTS.md</c> records.
/// </summary>
/// <remarks>
/// These are the only rules here that read model metadata rather than compiled types, and they
/// earn it because each one fails <em>quietly</em>: the model builds, migrations apply, rows are
/// written, and something is subtly wrong for a long time. A missing <c>ValueComparer</c> in
/// particular is invisible from the HTTP seam — a create-then-read round trip passes with or
/// without it, and what breaks instead is change tracking on a later update.
/// <para>
/// <b>Why the model is built here rather than taken from the context.</b> <c>context.Model</c> is
/// the finalised model, and finalisation consumes the <c>ValueComparer</c> and <c>ValueConverter</c>
/// annotations into runtime state. Worse for a rule, <c>GetValueComparer()</c> then answers for
/// every property, because EF synthesises a default where none was configured — so asking it
/// whether a comparer exists always says yes, and the rule would pass vacuously. Building the model
/// through <see cref="ModelBuilder"/> stops before that point, where the annotations still record
/// what the configuration actually declared. This was found by probing both, not by assuming.
/// </para>
/// <para>
/// No database is involved and no <c>DbContext</c> is constructed. Nothing here connects, so this
/// stays part of the fast, container-free suite.
/// </para>
/// </remarks>
public sealed class PersistenceModelTests
{
    /// <summary>
    /// Rule: <see cref="Rules.EntityKeysAreNeverDatabaseGenerated"/>. Identity is the domain's,
    /// minted with <c>Guid.CreateVersion7()</c> before anything is saved.
    /// </summary>
    [Fact]
    public void EntityKeys_EveryOne_IsMintedByTheDomainAndNotTheDatabase() =>
        Rule.Over(
            Rules.EntityKeysAreNeverDatabaseGenerated,
            MappedEntities(),
            entity =>
            {
                var generated = entity
                    .FindPrimaryKey()?.Properties
                    .Where(property => property.ValueGenerated != ValueGenerated.Never)
                    .Select(property => property.Name)
                    .ToList() ?? [];

                return generated.Count == 0
                    ? null
                    : $"has a database-generated key ({string.Join(", ", generated)}). The domain "
                      + "mints ids with Guid.CreateVersion7(), so its configuration needs "
                      + ".Property(x => x.Id).ValueGeneratedNever() - otherwise identity has two "
                      + "sources of truth and the aggregate never agreed to the winning one.";
            },
            entity => entity.ClrType.Name);

    /// <summary>
    /// Rule: <see cref="Rules.DomainEventsAreNeverMapped"/>. The symptom of forgetting is a
    /// DomainEvents table appearing in the next migration, which only someone reading the
    /// generated SQL would catch.
    /// </summary>
    [Fact]
    public void DomainEvents_OnEveryAggregateRoot_AreNotMapped() =>
        Rule.Over(
            Rules.DomainEventsAreNeverMapped,
            [.. MappedEntities().Where(entity => HasDomainEvents(entity.ClrType))],
            entity => entity.GetNavigations().Any(Named) || entity.GetProperties().Any(Named)
                ? "maps its DomainEvents collection. EF's relationship convention will invent a "
                  + "DomainEvents table with a foreign key. Add builder.Ignore(x => x.DomainEvents) "
                  + "to its configuration."
                : null,
            entity => entity.ClrType.Name);

    /// <summary>
    /// Rule: <see cref="Rules.ValueObjectsHaveAConverterAndComparer"/>. Both halves, because the
    /// converter alone compiles, maps, and reads back correctly — and leaves EF comparing value
    /// objects by reference.
    /// </summary>
    [Fact]
    public void ValueObjects_EveryMappedOne_HasAConverterAndAnExplicitComparer() =>
        Rule.Over(
            Rules.ValueObjectsHaveAConverterAndComparer,
            ValueObjectProperties(),
            property =>
            {
                if (property.FindAnnotation("ValueConverter")?.Value is null)
                {
                    return "has no ValueConverter. Value objects map through a converter, never "
                        + "ComplexProperty - EF 10 cannot bind a complex type to a constructor "
                        + "parameter, and every entity here has a private constructor taking value "
                        + "objects.";
                }

                return property.FindAnnotation("ValueComparer")?.Value is null
                    ? "has a ValueConverter but no explicit ValueComparer. EF then compares this "
                      + "value object by reference, so a change can go unwritten, or a write "
                      + "happen with nothing changed. The comparer is not optional: call "
                      + "property.Metadata.SetValueComparer(...) beside the conversion."
                    : null;
            },
            property => $"{property.DeclaringType.ClrType.Name}.{property.Name}");

    /// <summary>
    /// The model as the configurations declare it, built once and never finalised — see the note
    /// on this class for why that distinction is what makes these rules able to fail.
    /// </summary>
    private static IMutableModel Model { get; } = BuildModel();

    private static IMutableModel BuildModel()
    {
        var builder = new ModelBuilder(SqlServerConventionSetBuilder.Build());
        builder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);
        return builder.Model;
    }

    private static IReadOnlyCollection<IMutableEntityType> MappedEntities() =>
        [.. Model.GetEntityTypes()];

    private static bool HasDomainEvents(Type clrType) =>
        clrType.GetProperty(nameof(AggregateRoot<Guid>.DomainEvents)) is not null;

    private static bool Named(IReadOnlyPropertyBase member) =>
        member.Name == nameof(AggregateRoot<Guid>.DomainEvents);

    /// <summary>
    /// Every mapped property whose CLR type is a value object. Selected from the model rather than
    /// from the namespace, so an unmapped value object is out of scope and a mapped one cannot be
    /// missed.
    /// </summary>
    private static IReadOnlyCollection<IMutableProperty> ValueObjectProperties() =>
        [.. Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => typeof(ValueObject).IsAssignableFrom(property.ClrType))];
}
