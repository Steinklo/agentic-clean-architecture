# Todo.Api — the map

What this layer contains today. **The rules are in
[`docs/rules/layers/Todo.Api.md`](../../docs/rules/layers/Todo.Api.md)** and are not repeated here.

## Routes

**TodoLists** — base class `Endpoints/TodoLists/TodoListEndpoint.cs`, prefix `/api/todo-lists`,
OpenAPI tag `TodoLists`.

| Method | Route | Endpoint |
|---|---|---|
| `POST` | `/api/todo-lists` | `CreateTodoListEndpoint` |
| `GET` | `/api/todo-lists/{todoListId:guid}` | `GetTodoListEndpoint` |
| `POST` | `/api/todo-lists/{todoListId:guid}/items` | `AddTodoItemEndpoint` |
| `POST` | `/api/todo-lists/{todoListId:guid}/items/{todoItemId:guid}/complete` | `CompleteTodoItemEndpoint` |
| `POST` | `/api/todo-lists/{todoListId:guid}/archive` | `ArchiveTodoListEndpoint` |
| `GET` | `/api/todo-lists/{todoListId:guid}/incomplete-count` | `GetIncompleteItemCountEndpoint` |

`/health` is mapped in `Program.cs` and reports real database connectivity.

Every endpoint above dispatches through `ISender` to its Application-layer request, except
`GetIncompleteItemCountEndpoint`, which queries `TodoDbContext` directly and never calls
`CountIncompleteItemsQuery`.

## Plumbing

| | |
|---|---|
| `Endpoints/IEndpoint.cs` | the contract the assembly scan looks for |
| `Endpoints/EndpointExtensions.cs` | the scan, and one route group per prefix |
| `Common/ResultExtensions.cs` | the single `Result` → `IResult` translation |
| `Program.cs` | composition root |

## Tests

`tests/Todo.IntegrationTests/` — `TodoLists/` per feature, plus `HealthEndpointTests`.
