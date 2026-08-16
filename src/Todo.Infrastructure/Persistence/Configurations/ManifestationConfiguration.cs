using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain.Manifestations;

namespace Todo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Manifestation aggregate root.
/// </summary>
/// <remarks>
/// <para>
/// <b>No foreign key to TodoLists or TodoItems.</b> The ids are stored as plain columns, because
/// they are a reference across an aggregate boundary and a real foreign key here would let EF's
/// conventions grow a navigation - which is the boundary dissolving in the mapping rather than in
/// the model. The relationship is enforced by the use case that creates a Manifestation, which
/// checks the TodoItem exists before it does.
/// </para>
/// <para>
/// <b><see cref="ManifestationState"/> is stored as its name, not its ordinal.</b> Reordering or
/// inserting a member would otherwise silently re-label every existing row, and a database of
/// zeroes and ones tells a reader nothing.
/// </para>
/// </remarks>
internal sealed class ManifestationConfiguration : IEntityTypeConfiguration<Manifestation>
{
    /// <summary>The longest <see cref="ManifestationState"/> name, so the column is bounded.</summary>
    private static readonly int _stateMaxLength =
        Enum.GetNames<ManifestationState>().Max(name => name.Length);

    public void Configure(EntityTypeBuilder<Manifestation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Manifestations");

        builder.HasKey(manifestation => manifestation.Id);

        // The domain mints identities with Guid.CreateVersion7(), so the database must not.
        builder.Property(manifestation => manifestation.Id).ValueGeneratedNever();

        builder.Property(manifestation => manifestation.TodoListId).IsRequired();
        builder.Property(manifestation => manifestation.TodoItemId).IsRequired();

        builder
            .Property(manifestation => manifestation.State)
            .HasConversion<string>()
            .HasMaxLength(_stateMaxLength)
            .IsRequired();

        builder.Property(manifestation => manifestation.CreatedAt).IsRequired();

        // Domain events are recorded in memory and dispatched by the unit of work; they are not
        // state. Without this, EF's relationship convention maps the collection as a navigation
        // and invents a table for it. EVERY aggregate-root configuration must do this.
        builder.Ignore(manifestation => manifestation.DomainEvents);
    }
}
