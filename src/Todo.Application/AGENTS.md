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
| `CountIncompleteItemsQuery` | `TodoLists/CountIncompleteItems.cs` — loose at the feature root, not a use-case folder | `Result<int>` |

Each folder holds the request, its handler in the same file, and its validator beside them.

| | |
|---|---|
| DTOs | `TodoLists/Dtos/` — `TodoListDto`, `TodoItemDto` |
| Event handlers | `TodoLists/Events/` — one per domain event, plus `TodoListEventLog` (ids 2000–2002) |
| Repository contract | `TodoLists/Abstractions/ITodoListRepository.cs` |

## Common

| | |
|---|---|
| `Common/Behaviours/` | `UnhandledExceptionBehaviour`, `LoggingBehaviour`, `ValidationBehaviour`, `PerformanceBehaviour`, and `BehaviourLog` (ids 1000–1002) |
| `Common/Persistence/` | `IUnitOfWork` |
| `ConfigureServices.cs` | Mediator registration, the behaviour list in order, and validator scanning |

## Tests

Through the HTTP seam only: `tests/Todo.IntegrationTests/TodoLists/`.
