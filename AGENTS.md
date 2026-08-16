# AGENTS.md

**The map: what exists and where it lives.** Regenerated as code lands, so it describes the
solution as it is right now, never as it should be.

**The rules are in [`docs/`](docs/) and this file does not repeat them.** Read them there:

| | |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | the dependency rule, the enforced rules and their coverage model, the two testing seams, who enforces what |
| [`docs/conventions.md`](docs/conventions.md) | the stack, the commands, naming and code shape |
| [`docs/gotchas.md`](docs/gotchas.md) | the traps that already cost someone real time |
| [`docs/layers/<project>.md`](docs/layers/) | what belongs in each layer and what must not |
| [`docs/DOC-RULES.md`](docs/DOC-RULES.md) | how this file and its siblings are written |

Three skills carry the procedures: **`new-feature`** (any new type, slice or endpoint — it owns
where files go and in what order), **`add-migration`** (anything touching the schema),
**`git-hygiene`** (the protected-path check and the documentation agent's commits on your branch).

Each layer under `src/` carries its own map. Read the one for the layer you are editing.

## Features

**TodoLists** — the only feature. A `TodoList` owns `TodoItem`s and refuses to be archived while
any of them is incomplete.

| | |
|---|---|
| Aggregate | `src/Todo.Domain/TodoLists/` — `TodoList`, child entity `TodoItem`, value objects `TodoListTitle` and `TodoItemDescription`, events for created / item-completed / archived |
| Use cases | `src/Todo.Application/TodoLists/` — commands `CreateTodoList`, `AddTodoItem`, `CompleteTodoItem`, `ArchiveTodoList`; query `GetTodoList` |
| Persistence | `src/Todo.Infrastructure/Persistence/` — `TodoListConfiguration`, `TodoItemConfiguration`, `TodoListRepository` |
| HTTP | `src/Todo.Api/Endpoints/TodoLists/` — one endpoint per use case, under `/api/todo-lists` |
| Tests | `tests/Todo.IntegrationTests/TodoLists/` and `tests/Todo.UnitTests/TodoLists/` |

## Entry points

| | |
|---|---|
| `src/Todo.Api/Program.cs` | composition root; calls each layer's `Add<Layer>Services(...)` |
| `src/Todo.Application/ConfigureServices.cs` | Mediator, its behaviour list, and validator scanning |
| `src/Todo.Infrastructure/ConfigureServices.cs` | the `DbContext`, repositories and unit of work |
| `src/Todo.Api/Common/ResultExtensions.cs` | the single `Result` → `IResult` translation |
| `tests/Todo.ArchitectureTests/Rules.cs` | every enforced rule, and how much each currently proves |

## Shared building blocks

`src/Todo.Domain/Common/` holds `Result`, `DomainError`, `DomainErrorType`, and the `Entity`,
`AggregateRoot`, `ValueObject` and `DomainEvent` bases every feature derives from.

`src/Todo.Application/Common/Behaviours/` holds the four pipeline behaviours — unhandled
exception, logging, validation, performance — and `Common/Persistence/IUnitOfWork.cs`.

## Elsewhere

- Domain glossary: [`CONTEXT.md`](CONTEXT.md) — use its vocabulary, and challenge terms that
  conflict with it.
- Decision records: [`docs/adr/`](docs/adr/), for decisions taken on top of this template.
