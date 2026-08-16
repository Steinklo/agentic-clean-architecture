# Todo.Domain — the rules

The innermost layer: the model and its invariants. It knows nothing about how it is stored,
transported or hosted.

**How to add an aggregate root, value object, child entity or domain event: the `new-feature`
skill.** It owns the shapes; this file owns what may exist here at all.

## What belongs

Aggregate roots, child entities, value objects, domain events, and the rules that must hold
regardless of who is asking. `Result`, `DomainError` and the shared base types under `Common/`.

## What must not

- **No project references at all** — `Rules.DomainHasNoProjectReferences`.
- **No ORM types** — `Rules.DomainUsesNoOrmTypes`.
- **No public property setters** — `Rules.EntitiesHaveNoPublicSetters`.
- **Nothing in a `ValueObjects` namespace that does not derive from `ValueObject`** —
  `Rules.ValueObjectsDeriveFromValueObject`.
- **Every aggregate root derives from `AggregateRoot<Guid>`** —
  `Rules.AggregateRootsAreKeyedOnGuid`. Another key compiles and saves, and its events are never
  dispatched, because `UnitOfWork` enumerates that closed generic.
- No package beyond `Mediator.Abstractions`, which exists solely because `DomainEvent` implements
  `INotification`.
- No persistence concerns — no attributes, no rehydration factories, no `InternalsVisibleTo`. EF
  binds the private constructors and backing fields directly.
- No central `*Errors` class. Errors are constructed inline at the guard that rejects, and
  `ErrorCodeUniquenessTests` scans this project's source to prove no two guards share a code.

## Advice, not rules

Neither is enforced: expressing them as a test would need a pluralisation heuristic and a guess at
which methods count, and a rule that is wrong one time in ten trains people to override it. So
weigh them rather than assuming something checks.

- **Folders are plural, type names singular.** The folder is `TodoLists`; the types in it are
  `TodoList`, `TodoListTitle`, `TodoListCreatedEvent`. Pluralising a type name to match its folder
  is the mistake. The feature namespace is plural precisely because the singular collides with the
  aggregate's own class name.
- **A child entity's `Create` and every mutator are `internal`**, so the compiler — not a
  convention — makes the aggregate root the only way in. The root calls them, adds to its backing
  list, and raises any event itself.

## Tests

Aggregate seam only, in `tests/Todo.UnitTests`: construct the aggregate, assert on the returned
`Result`. If a test here needs a test double, the invariant has leaked out of the aggregate.
