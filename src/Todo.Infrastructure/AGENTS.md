# Todo.Infrastructure — the map

What this layer contains today. **The rules are in
[`docs/layers/Todo.Infrastructure.md`](../../docs/layers/Todo.Infrastructure.md)** and are not
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

## Reality

`Reality/RealityGateway.cs` — the only implementation of `Todo.Application.Manifestations.IRealityGateway`.
It declines every attempt with `DomainError.NotImplemented`; there is no adapter to the physical world.

## Migrations

`Persistence/Migrations/` — `InitialCreate`, `AddTodoList`, `AddTodoItem`, `AddManifestation`, and
the model snapshot. An applied migration is never edited; a local hook refuses the write.

## Composition

`ConfigureServices.cs` — the `DbContext` against SQL Server, `IDatabaseConnectivity`, one
`AddScoped` per repository plus the unit of work, and the reality gateway port. The only file a
new aggregate or port adds a DI line to.
