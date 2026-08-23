# Todo.Application — the map

What this layer contains today. **The rules are in
[`docs/rules/layers/Todo.Application.md`](../../docs/rules/layers/Todo.Application.md)** and are not repeated
here.

## Features

**TodoLists** (`TodoLists/`)

| Use case | Folder | Answers with |
|---|---|---|
| `CreateTodoListCommand` | `Commands/CreateTodoList/` | `Result<TodoListDto>` |
| `AddTodoItemCommand` | `Commands/AddTodoItem/` | `Result<TodoItemDto>` |
| `CompleteTodoItemCommand` | `Commands/CompleteTodoItem/` | `Result` |
| `ArchiveTodoListCommand` | `Commands/ArchiveTodoList/` | `Result` |
| `GetTodoListQuery` | `Queries/GetTodoList/` | `Result<TodoListDto>` |

Each folder holds the request, its handler in the same file, and its validator beside them.

| | |
|---|---|
| DTOs | `TodoLists/Dtos/` — `TodoListDto`, `TodoItemDto` |
| Event handlers | `TodoLists/Events/` — one per domain event, plus `TodoListEventLog` (ids 2000–2002) |
| Repository contract | `TodoLists/Abstractions/ITodoListRepository.cs` |

**Manifestations** (`Manifestations/`)

| Use case | Folder | Answers with |
|---|---|---|
| `RequestManifestationCommand` | `Commands/RequestManifestation/` | `Result<ManifestationDto>` |
| `FulfilManifestationCommand` | `Commands/FulfilManifestation/` | `Result` |
| `GetManifestationQuery` | `Queries/GetManifestation/` | `Result<ManifestationDto>` |

Each folder holds the request, its handler in the same file, and its validator beside them.

| | |
|---|---|
| DTOs | `Manifestations/Dtos/` — `ManifestationDto` |
| Event handlers | `Manifestations/Events/` — one per domain event, plus `ManifestationEventLog` (ids 3000–3003) |
| Repository contract | `Manifestations/Abstractions/IManifestationRepository.cs` |
| Gateway contract | `Manifestations/Abstractions/IRealityGateway.cs` |

## Common

| | |
|---|---|
| `Common/Behaviours/` | `UnhandledExceptionBehaviour`, `LoggingBehaviour`, `ValidationBehaviour`, `PerformanceBehaviour`, and `BehaviourLog` (ids 1000–1002) |
| `Common/Persistence/` | `IUnitOfWork` |
| `ConfigureServices.cs` | Mediator registration, the behaviour list in order, and validator scanning |

## Tests

Through the HTTP seam only: `tests/Todo.IntegrationTests/TodoLists/`.
