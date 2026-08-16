# Todo.Infrastructure — the rules

Everything that talks to the outside world. The **only** layer permitted to know about the
database.

**How to add a configuration or repository: the `new-feature` skill. Anything that changes the
schema: `add-migration`.**

## What belongs

`TodoDbContext` and every `IEntityTypeConfiguration<T>`; the repository and unit-of-work
implementations of the interfaces declared in Application; the migrations; and
`AddInfrastructureServices(...)`, the single entry point the Api composes and the one place a new
aggregate's repository is registered by hand.

## What must not

- No use-case logic. If it decides something, it belongs in Application or Domain.
- No domain invariants. Reconstituting an aggregate must not re-run validation — the data already
  passed it.
- No `DbSet` properties on `TodoDbContext`, and no edits to it at all: configurations arrive by
  assembly scan and repositories use `_context.Set<T>()`, so adding an aggregate adds files and
  changes none.
- **Every repository interface is registered here** — `Rules.RepositoriesAreRegistered`. It is the
  one wiring step nothing discovers: the build stays green and the first request throws.

## Mapping

The domain model is mapped **directly**: no persistence POCOs, no rehydration factories. EF binds
private constructors and backing fields, so a parallel set of classes would add objects and
mapping code and buy nothing. Child collections bind to the private backing field and normally
need no navigation configuration.

Three of the EF traps in [`../gotchas.md`](../gotchas.md) are rules rather than warnings, and every
one of them fails quietly: the model builds, migrations apply, rows are written, and something is
wrong for a long time.

- **`Rules.ValueObjectsHaveAConverterAndComparer`** — a `ValueConverter` *and* an explicit
  `ValueComparer`, never `ComplexProperty`. The converter alone reads back correctly and leaves EF
  comparing by reference.
- **`Rules.EntityKeysAreNeverDatabaseGenerated`** — `ValueGeneratedNever()` on every key. The
  domain mints ids.
- **`Rules.DomainEventsAreNeverMapped`** — `Ignore(x => x.DomainEvents)` on every aggregate root,
  or EF invents a table for it.

These read the model as the configurations declare it, before EF finalises it. That matters if you
ever write a rule like them: the finalised model synthesises a default comparer for every
property, so asking whether one exists always answers yes.

## Unit of work

Domain events are dispatched **immediately before** the underlying save, so a handler's changes
join the same transaction. Events are cleared before publishing — that is what terminates the loop
— and the change tracker is re-scanned, so events raised by handlers are picked up. Only
`AggregateRoot<Guid>` entries are scanned.
