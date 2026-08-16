using Microsoft.EntityFrameworkCore;

namespace Todo.Infrastructure.Persistence;

/// <summary>
/// The application's single Entity Framework Core context.
/// </summary>
/// <remarks>
/// No entity types are mapped here. Every mapping decision lives in an
/// <see cref="IEntityTypeConfiguration{TEntity}"/> in this assembly and is picked up by
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly, System.Func{System.Type, bool})"/>
/// below, so adding an aggregate never requires editing this file.
/// </remarks>
public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodoDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
