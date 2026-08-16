---
name: add-migration
description: Add and review an EF Core migration in this solution. Use after changing an entity, a value object, or an IEntityTypeConfiguration in Todo.Infrastructure, when running dotnet ef, or when a schema change needs to reach the database.
---

# Add a migration

EF Core 10.0.11, SQL Server, single `TodoDbContext`. Every command runs from the repository root.

## 1. Land the model change first

The migration is generated from the compiled model, so the entity in `src/Todo.Domain` and its `IEntityTypeConfiguration<T>` in `src/Todo.Infrastructure/Persistence/Configurations/` must already exist and compile. The `new-feature` skill has the shape of both.

```bash
dotnet build      # 0 warnings, or the generator sees stale IL
```

## 2. Generate

Name it PascalCase and verb-first, matching the existing names — `InitialCreate`, `AddTodoList`, `AddTodoItem`. EF prefixes the timestamp; write no date yourself.

```bash
dotnet ef migrations add <Name> \
  --project src/Todo.Infrastructure \
  --startup-project src/Todo.Infrastructure \
  --output-dir Persistence/Migrations
```

Three details, all load-bearing:

- **`--startup-project` is Infrastructure, not `Todo.Api`.** `Todo.Api` is deliberately EF-free and carries no `Microsoft.EntityFrameworkCore.Design` reference; pointing at it fails with `Your startup project 'Todo.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.` Do not "fix" that by adding the package to Api — it would break the architecture test forbidding ORM namespaces there. `TodoDbContextFactory` (an `IDesignTimeDbContextFactory<TodoDbContext>`) is what lets Infrastructure be its own startup project.
- **`--output-dir Persistence/Migrations`.** EF defaults to `Migrations/` at the project root, which is the wrong folder here.
- **No `--framework`.** EF 10 needs it on a project targeting more than one framework, and `--help` documents the default as "the first one in the project" — so on a multi-target project omitting it silently picks one rather than failing. This solution is single-target (`<TargetFramework>net10.0</TargetFramework>` in `Directory.Build.props`), so the flag does not apply. Pass it the moment `TargetFrameworks` (plural) appears anywhere.

`migrations add` opens no database connection. Commands that do read `ConnectionStrings__TodoDb` from the environment, falling back to the design-time string in `TodoDbContextFactory`.

## 3. Read the generated SQL before committing

```bash
dotnet ef migrations script <PreviousMigrationName> <NewMigrationName> \
  --project src/Todo.Infrastructure \
  --startup-project src/Todo.Infrastructure
```

Add `--idempotent --output migrations.sql` to script the whole chain the way a deployment would apply it. Write the file outside the repository.

Beyond the usual reading for data loss, four things here mean something specific:

- **An empty `Up()`.** Your model change never reached the model: the configuration is not being discovered by `ApplyConfigurationsFromAssembly`. Check it is in `Todo.Infrastructure` and implements `IEntityTypeConfiguration<T>` closed over the right type. (`InitialCreate` is genuinely empty; a new one must not be.)
- **A `DomainEvents` table.** An aggregate-root configuration is missing `builder.Ignore(x => x.DomainEvents)`.
- **A second table, or a shadow key, for a value object.** It was mapped as an owned or complex type instead of through a converter — see step 5.
- **An identity or sequence on a primary key.** A configuration is missing `.Property(x => x.Id).ValueGeneratedNever()`. The domain mints ids with `Guid.CreateVersion7()`, so the database must not.

## 4. Verify it applies

```bash
dotnet test tests/Todo.IntegrationTests
```

The fixture applies migrations against a real SQL Server container. Needs Docker and an x86-64 host — there is no ARM SQL Server image.

Commit the migration `.cs`, its `.Designer.cs`, and `TodoDbContextModelSnapshot.cs` together with the configuration change, in one commit.

## 5. Value objects: converter **and** comparer

The `new-feature` skill's *A new value object* section has the code. What the migration must show is **one inline column** in the owning entity's own table — no second table, no shadow key — `NOT NULL`, because value objects are required here and never optional. A second table or a shadow key means it was mapped as an owned or complex type; `ComplexProperty` in particular fails at model build with `No suitable constructor was found`, before any migration is generated.

**The one thing the migration cannot tell you is the one thing most easily missed.** With `HasConversion` but no `property.Metadata.SetValueComparer(...)`, the column, its type, its length and the generated SQL are all correct while the change tracker compares by reference — updates silently missed or spuriously issued. Check the configuration, not the SQL.

## 6. An applied migration is never edited

**Applied** means merged, pushed, or run against any database other than a throwaway local one.

Editing one changes nothing for a database that already recorded it in `__EFMigrationsHistory` — those never re-run it — while fresh databases get the new version. The two schemas diverge permanently, the model snapshot describes neither, and the next migration is generated against a model matching no live database. There is no recovery short of hand-repairing every environment. **Add a new migration instead**: wrong column name, wrong type, missing index are all new migrations.

**This is enforced.** A `PreToolUse` hook (`.claude/hooks/applied-migrations.mjs`) refuses any edit to a file under a `Migrations/` folder that already exists on disk, including `.Designer.cs` files and the model snapshot. Creating one is unaffected — `dotnet ef migrations add` writes through the CLI, which the hook never sees, and a path that does not yet exist is allowed. It is the one rule here a hook owns outright rather than mirroring from CI, because a diff touching a migration is legitimate when the migration is new and illegitimate when it is not, and only the local filesystem knows which at the moment of the edit. To edit one anyway, set `APPLIED_MIGRATIONS_BYPASS=1` — and be sure it has never been applied anywhere.

The one legitimate undo, for a migration still local and unpushed:

```bash
dotnet ef migrations remove \
  --project src/Todo.Infrastructure \
  --startup-project src/Todo.Infrastructure \
  --force
```

It deletes the migration files and reverts the model snapshot. `--force` skips the "has this been applied?" check, which cannot run without a reachable database — without it the command reports `Unable to check if the migration '<id>' has been applied to the database` and stops. Then fix the configuration and go back to step 2.
