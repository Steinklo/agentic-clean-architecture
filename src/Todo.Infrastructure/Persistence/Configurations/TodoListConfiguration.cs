using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain.TodoLists;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the TodoList aggregate root. The domain type is mapped directly, with no persistence
/// model in between: EF binds private constructors and backing fields, so a parallel set of
/// POCOs would add objects and mapping code and buy nothing.
/// </summary>
internal sealed class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TodoLists");

        builder.HasKey(todoList => todoList.Id);

        // The domain mints identities with Guid.CreateVersion7(), so the database must not.
        builder.Property(todoList => todoList.Id).ValueGeneratedNever();

        ConfigureTitle(builder);

        builder.Property(todoList => todoList.CreatedAt).IsRequired();
        builder.Property(todoList => todoList.IsArchived).IsRequired();
        builder.Property(todoList => todoList.ArchivedAt);

        // Domain events are recorded in memory and dispatched by the unit of work; they are not
        // state. Without this, EF's relationship convention maps the collection as a navigation
        // and invents a table for it. EVERY entity configuration in this assembly must do this.
        builder.Ignore(todoList => todoList.DomainEvents);

        // Items is deliberately absent. It is a real navigation - see TodoItemConfiguration - and
        // convention maps it correctly on its own: the read-only property is written through the
        // private _items backing field under the default PropertyAccessMode.PreferField, and
        // TodoItem.TodoListId is discovered as its foreign key. Nothing to state here.
    }

    /// <summary>
    /// Stores <see cref="TodoListTitle"/> inline, in the aggregate's own table, as a single
    /// required <c>Title</c> column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Read this before mapping a value object in ticket 06 or 07.</b>
    /// </para>
    /// <para>
    /// <c>ComplexProperty</c> is the natural mechanism for a value object, and on EF 10 it is not
    /// possible for the domain model as it stands. The failure is a hard one, at model build time:
    /// </para>
    /// <code>
    /// No suitable constructor was found for the type 'TodoList'.
    ///   Cannot bind 'title' in 'TodoList(Guid id, TodoListTitle title)'
    /// </code>
    /// <para>
    /// EF binds a constructor parameter only to a <em>scalar</em> property of the type - see
    /// <c>PropertyParameterBindingFactory.FindParameter</c>, which searches
    /// <c>entityType.GetProperties()</c> and never the complex ones. Binding a complex type to a
    /// constructor parameter is efcore#31621, open and on the backlog, so it is absent from EF 10
    /// and EF 11 alike. <c>TodoList</c>'s only constructor takes a <see cref="TodoListTitle"/>, so
    /// declaring <c>Title</c> as a complex property leaves EF unable to construct a TodoList at
    /// all. The workarounds all require internal EF APIs (<c>SetConstructorBinding</c> and a
    /// hand-built <c>ParameterBinding</c>), which do not belong in a template.
    /// </para>
    /// <para>
    /// The mapping below is the supported equivalent: a value converter to the value object's one
    /// component, plus a value comparer so the change tracker compares by value rather than by
    /// reference. The column, its type, its length and its nullability are identical to what
    /// <c>ComplexProperty</c> would have produced, and no owned entity, second table or shadow key
    /// is created - so nothing that mapping the domain directly is meant to buy is given up.
    /// </para>
    /// <para>
    /// To restore complex types, <c>TodoList</c>'s private constructor has to stop taking value
    /// objects and take only the entity's scalars, letting EF write <c>Title</c> to its property
    /// after construction. That is a change to Todo.Domain, which this ticket does not own.
    /// </para>
    /// </remarks>
    private static void ConfigureTitle(EntityTypeBuilder<TodoList> builder)
    {
        var titleProperty = builder.Property(todoList => todoList.Title);

        titleProperty
            .HasConversion(
                title => title.Value,

                // Reading goes back through the domain factory, so a row that somehow violates the
                // domain's rules fails loudly here rather than becoming an invalid aggregate.
                value => TodoListTitle.Create(value).Value)
            .HasColumnName("Title")
            .HasMaxLength(TodoListTitle.MaxLength)
            .IsRequired();

        // Value objects compare by value and are immutable, so the snapshot is the instance itself.
        titleProperty.Metadata.SetValueComparer(
            new ValueComparer<TodoListTitle>(
                (left, right) => left == right,
                title => title.GetHashCode(),
                title => title));
    }
}
