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

**Manifestations** — base classes `Endpoints/Manifestations/ManifestationEndpoint.cs`, prefix
`/api/manifestations`, tag `Manifestations`, and (for the one route that is a sub-resource of a
TodoItem) `Endpoints/TodoLists/TodoListEndpoint.cs`.

| Method | Route | Endpoint |
|---|---|---|
| `POST` | `/api/todo-lists/{todoListId:guid}/items/{todoItemId:guid}/manifest` | `RequestManifestationEndpoint` |
| `GET` | `/api/manifestations/{manifestationId:guid}` | `GetManifestationEndpoint` |
| `POST` | `/api/manifestations/{manifestationId:guid}/fulfil` | `FulfilManifestationEndpoint` |

`/health` is mapped in `Program.cs` and reports real database connectivity.

## Plumbing

| | |
|---|---|
| `Endpoints/IEndpoint.cs` | the contract the assembly scan looks for |
| `Endpoints/EndpointExtensions.cs` | the scan, and one route group per prefix |
| `Common/ResultExtensions.cs` | the single `Result` → `IResult` translation |
| `Program.cs` | composition root |

## Tests

`tests/Todo.IntegrationTests/` — `TodoLists/` and `Manifestations/` per feature, plus
`HealthEndpointTests`.
