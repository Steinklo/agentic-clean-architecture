# Todo.Infrastructure — the map

What this layer contains today. **The rules are in
[`docs/rules/layers/Todo.Infrastructure.md`](../../docs/rules/layers/Todo.Infrastructure.md)** and are not
repeated here.

## Persistence

| | |
|---|---|
| Context | `Persistence/TodoDbContext.cs` — no `DbSet`s; configurations arrive by assembly scan |
| Design-time | `Persistence/TodoDbContextFactory.cs`, for `dotnet ef` |
| Configurations | `Persistence/Configurations/` — `TodoListConfiguration`, `TodoItemConfiguration`, `ManifestationConfiguration` |
| Repositories | `Persistence/Repositories/` — `TodoListRepository`, `ManifestationRepository` |
| Unit of work | `Persistence/UnitOfWork.cs` — dispatches domain events immediately before the save |
| Connectivity | `Persistence/IDatabaseConnectivity.cs` and its implementation, behind `/health` |
| Migration on startup | `Persistence/DatabaseMigrator.cs`, gated by `Database:MigrateOnStartup` |

## Migrations

`Persistence/Migrations/` — `InitialCreate`, `AddTodoList`, `AddTodoItem`, `AddManifestation`, and
the model snapshot. An applied migration is never edited; a local hook refuses the write.

## Reality

`Reality/RealityGateway.cs` — the only `IRealityGateway` implementation. It always declines.

## Composition

`ConfigureServices.cs` — the `DbContext` against SQL Server, `IDatabaseConnectivity`, one
`AddScoped` per repository plus the unit of work, and `IRealityGateway`. The only file a new
aggregate or gateway adds a DI line to.
