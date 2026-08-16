# Todo.Domain — the map

What this layer contains today. **The rules are in
[`docs/layers/Todo.Domain.md`](../../docs/layers/Todo.Domain.md)** and are not repeated here.

## Features

**TodoLists** (`TodoLists/`)

| | |
|---|---|
| Aggregate root | `TodoList` — creates, adds and completes items, archives |
| Child entity | `Entities/TodoItem` — created and completed only through its list |
| Value objects | `ValueObjects/TodoListTitle`, `ValueObjects/TodoItemDescription` |
| Events | `Events/TodoListCreatedEvent`, `TodoItemCompletedEvent`, `TodoListArchivedEvent` |

The invariant the aggregate boundary exists for: `TodoList.Archive()` refuses while any
`TodoItem` is incomplete.

**Manifestations** (`Manifestations/`)

| | |
|---|---|
| Aggregate root | `Manifestation` — requests, then settles once via `Realize()` or `Fail()` |
| State | `ManifestationState` — `Requested`, and the terminal `Realized` / `Failed` |
| Events | `Events/ManifestationRequestedEvent`, `ManifestationRealizedEvent`, `ManifestationFailedEvent` |

The invariant the aggregate exists for: a settled `Manifestation` refuses a second transition.
`Manifestation` refers to its `TodoItem` and `TodoListId` by id only, never by navigation.

## Common

`Common/` — `Result` and `Result<T>`, `DomainError`, `DomainErrorType`, and the `Entity<TId>`,
`AggregateRoot<TId>`, `ValueObject` and `DomainEvent` base types.

## Tests

`tests/Todo.UnitTests/TodoLists/TodoListTests.cs`, `tests/Todo.UnitTests/Manifestations/ManifestationTests.cs`,
plus `tests/Todo.UnitTests/Common/ErrorCodeUniquenessTests.cs`, which scans this project's source.
