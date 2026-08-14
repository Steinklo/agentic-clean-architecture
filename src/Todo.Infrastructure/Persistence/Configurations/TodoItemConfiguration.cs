using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain.TodoLists;
using Todo.Domain.TodoLists.Entities;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps TodoItem, the TodoList aggregate's child entity.
/// </summary>
/// <remarks>
/// <para>
/// A child entity, not an aggregate root: it has its own table and its own key, but it is only ever
/// reached through <see cref="TodoList"/>, which is why there is no <c>DbSet</c> and no repository
/// for it. The relationship itself is left to convention - <c>TodoItem.TodoListId</c> is discovered
/// as the foreign key for <c>TodoList.Items</c>, and the collection is written through its private
/// <c>_items</c> backing field, which EF finds under the default
/// <see cref="PropertyAccessMode.PreferField"/>. Configuring the navigation by hand would state
/// what convention already gets right; what would need stating is any departure from it.
/// </para>
/// <para>
/// There is no <c>Ignore</c> for domain events here, unlike <see cref="TodoListConfiguration"/>:
/// events belong to the aggregate root, so a TodoItem has no such collection to exclude.
/// </para>
/// </remarks>
internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TodoItems");

        builder.HasKey(todoItem => todoItem.Id);

        // The domain mints identities with Guid.CreateVersion7(), so the database must not.
        builder.Property(todoItem => todoItem.Id).ValueGeneratedNever();

        builder.Property(todoItem => todoItem.TodoListId).IsRequired();

        ConfigureDescription(builder);

        builder.Property(todoItem => todoItem.CreatedAt).IsRequired();
        builder.Property(todoItem => todoItem.IsComplete).IsRequired();
        builder.Property(todoItem => todoItem.CompletedAt);
    }

    /// <summary>
    /// Stores <see cref="TodoItemDescription"/> inline, in the TodoItem's own table, as a single
    /// required <c>Description</c> column.
    /// </summary>
    /// <remarks>
    /// The same shape as <c>TodoListConfiguration.ConfigureTitle</c>, and for the same reason:
    /// EF 10 cannot bind a complex type to a constructor parameter (efcore#31621), and
    /// <c>TodoItem</c>'s only constructor takes a <see cref="TodoItemDescription"/>, so a
    /// <c>ComplexProperty</c> here fails at model build with <i>"No suitable constructor was
    /// found"</i>. Read that method's remarks before mapping the next value object.
    /// <para>
    /// The <see cref="ValueComparer{T}"/> is not optional. Without it EF compares the converted
    /// value object by reference, so a description replaced with an equal one looks changed and a
    /// mutated one can look unchanged - silently wrong change tracking rather than a build failure.
    /// </para>
    /// </remarks>
    private static void ConfigureDescription(EntityTypeBuilder<TodoItem> builder)
    {
        var descriptionProperty = builder.Property(todoItem => todoItem.Description);

        descriptionProperty
            .HasConversion(
                description => description.Value,

                // Reading goes back through the domain factory, so a row that somehow violates the
                // domain's rules fails loudly here rather than becoming an invalid TodoItem.
                value => TodoItemDescription.Create(value).Value)
            .HasColumnName("Description")
            .HasMaxLength(TodoItemDescription.MaxLength)
            .IsRequired();

        // Value objects compare by value and are immutable, so the snapshot is the instance itself.
        descriptionProperty.Metadata.SetValueComparer(
            new ValueComparer<TodoItemDescription>(
                (left, right) => left == right,
                description => description.GetHashCode(),
                description => description));
    }
}
