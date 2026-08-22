using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Manifestation aggregate root.
/// </summary>
/// <remarks>
/// <para>
/// There is no relationship to TodoList or TodoItem here, and that is deliberate rather than
/// missing. The two ids are plain columns, not foreign keys: a Manifestation and a TodoList are
/// separate aggregates with separate lifecycles, and a database constraint spanning them would make
/// one unable to change without the other's permission — the aggregate boundary enforced everywhere
/// except in the schema, which is where it would actually bite.
/// </para>
/// <para>
/// The aggregate carries no value object, so there is no converter or comparer to write. Its only
/// non-scalar is the state enum, below.
/// </para>
/// </remarks>
internal sealed class ManifestationConfiguration : IEntityTypeConfiguration<Manifestation>
{
    /// <summary>
    /// Long enough for every <c>ManifestationState</c> name with room to spare, and short enough
    /// that the column stays an index-friendly width if one is ever wanted.
    /// </summary>
    private const int StateMaxLength = 32;

    public void Configure(EntityTypeBuilder<Manifestation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Manifestations");

        builder.HasKey(manifestation => manifestation.Id);

        // The domain mints identities with Guid.CreateVersion7(), so the database must not.
        builder.Property(manifestation => manifestation.Id).ValueGeneratedNever();

        builder.Property(manifestation => manifestation.TodoListId).IsRequired();
        builder.Property(manifestation => manifestation.TodoItemId).IsRequired();

        // Stored as its name, not its ordinal. A row then says what it means when read by hand, and
        // inserting a member into the enum later cannot silently reinterpret rows already written.
        builder
            .Property(manifestation => manifestation.State)
            .HasConversion<string>()
            .HasMaxLength(StateMaxLength)
            .IsRequired();

        builder.Property(manifestation => manifestation.CreatedAt).IsRequired();

        // Domain events are dispatched, not stored. Without this, EF's relationship convention maps
        // the collection as a navigation and invents a table for it.
        builder.Ignore(manifestation => manifestation.DomainEvents);
    }
}
