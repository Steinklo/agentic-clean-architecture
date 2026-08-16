---
name: new-feature
description: Build anything new in this solution - an aggregate root, a value object, a child entity, a domain event and its handler, a command, a query, a validator, a DTO, an endpoint and the tests. Use when adding a feature, a use case, a Mediator request or handler, a domain type or an endpoint, and when deciding where a rule, a folder or a status code belongs.
---

# Add to this solution

Work **inside out** — Domain, Application, Infrastructure, Api, tests — because each step compiles against the one before it. Every command runs from the repository root. `TodoLists` is the only feature, so every shape below has a worked instance in it; open the nearest one first.

A use case on an existing aggregate needs sections 1 and 5–11. A new aggregate root needs all of them. A value object or child entity on its own needs section 1, its own section, then the `add-migration` skill.

## 1. Where files go

Feature per aggregate, then a folder per use case. `<Feature>` is the **plural** of the aggregate (`TodoLists` for `TodoList`), because the singular collides with the class name. Namespaces follow folders.

**Folders are plural, type names are singular.** The folder is `TodoLists`; the types inside it are `TodoListEndpoint`, `TodoListEventLog`, `ITodoListRepository`, `TodoListDto`, `TodoListConfiguration`, `TodoListRepository`. Pluralising a type name to match its folder is the mistake to avoid.

| File | Path |
|---|---|
| Aggregate root | `src/Todo.Domain/<Feature>/<Aggregate>.cs`, at the feature root |
| Child entity / value object / domain event | `src/Todo.Domain/<Feature>/` then `Entities/`, `ValueObjects/`, `Events/` |
| Command or query: request + handler in one file, and its validator beside it | `src/Todo.Application/<Feature>/Commands/<UseCase>/` or `Queries/<UseCase>/` |
| DTO | `src/Todo.Application/<Feature>/Dtos/` — **not** the use-case folder |
| Domain-event handler, and `<Aggregate>EventLog.cs` | `src/Todo.Application/<Feature>/Events/` |
| Repository interface | `src/Todo.Application/<Feature>/I<Aggregate>Repository.cs`, at the feature root |
| Pipeline behaviour | `src/Todo.Application/Common/Behaviours/` |
| EF configuration | `src/Todo.Infrastructure/Persistence/Configurations/<Type>Configuration.cs` |
| Repository implementation | `src/Todo.Infrastructure/Persistence/Repositories/` |
| Endpoints, and the feature's `<Aggregate>Endpoint.cs` base class | `src/Todo.Api/Endpoints/<Feature>/` |
| HTTP tests | `tests/Todo.IntegrationTests/<Feature>/<Thing>EndpointTests.cs` |
| Aggregate tests | `tests/Todo.UnitTests/<Feature>/<Aggregate>Tests.cs` |

**Create the use-case folder**; files loose in `Commands/` are wrong (`Rules.RequestsLiveInAUseCaseFolder`). DTOs are the exception because they are shared — `TodoListDto` is returned by both `CreateTodoList` and `GetTodoList`, so it belongs to neither folder (`Rules.DtosLiveInTheirFeaturesDtosNamespace`).

Namespaces follow folders and a rule holds them to it (`Rules.NamespacesFollowFolders`), which is what lets the folder rules above assert on a namespace and mean a directory. Move a file and its namespace changes with it.

## 2. A new aggregate root

Copy `src/Todo.Domain/TodoLists/TodoList.cs`. It must have all of:

- `public sealed class <Aggregate> : AggregateRoot<Guid>`. **`Guid`, not another id type** — `UnitOfWork` enumerates `ChangeTracker.Entries<AggregateRoot<Guid>>()`, so an aggregate keyed on anything else compiles, saves, and never dispatches an event (`Rules.AggregateRootsAreKeyedOnGuid`).
- A **private** constructor taking `Guid id` and its value objects, chaining `: base(id)`. Never a parameterless one beside it: EF binds the constructor with the fewest property parameters, so an empty one would win.
- Every property `{ get; private set; }` (`Rules.EntitiesHaveNoPublicSetters`). Child collections are `IReadOnlyCollection<T>` over a `private readonly List<T> _items = [];` you initialise yourself; EF writes the field.
- `public static Result<<Aggregate>> Create(...)` taking **scalars only**: call each value object's `Create`, return `Result.Failure<T>(x.Error)` on the first failure, mint the id with `Guid.CreateVersion7()`, `RaiseDomainEvent(new <Aggregate>CreatedEvent(...))`, return `Result.Success(instance)`.
- Every state transition is a method returning `Result` or `Result<T>`, with its `DomainError` constructed **inline at the guard that rejects**. Choose the category deliberately — it is the only thing deciding the caller's status: `Validation` → 400, `NotFound` → 404, `Conflict` → 409, `NotImplemented` → 501, `Failure` → 500. A refused transition is a `Conflict`. `NotImplemented` is an **adapter's** answer, not an aggregate's — a port whose implementation cannot do the work yet returns it rather than throwing `NotImplementedException`, which would be caught by `UnhandledExceptionBehaviour` and become a 500 saying the application broke.
- Dotted error codes, unique across `Todo.Domain` — `ErrorCodeUniquenessTests` scans its source for `DomainError.<Category>("literal"` and fails on a repeat. One rule rejecting at two entry points is written once in a private helper returning `DomainError?`, as `TodoList.ArchivedRejection()` does.

In the same change it then needs a creation event and handler (§5), an EF configuration (§2b), a repository (§8), a migration (`add-migration` skill), and an endpoint base class (§10).

`TodoDbContext` is never edited: no `DbSet` properties, configurations arrive by `ApplyConfigurationsFromAssembly`, repositories use `_context.Set<T>()`.

### 2b. Its EF configuration

`internal sealed class <Aggregate>Configuration : IEntityTypeConfiguration<<Aggregate>>`, copying `TodoListConfiguration`. `ArgumentNullException.ThrowIfNull(builder)` first, then:

- `builder.ToTable("<Plural>")`, `builder.HasKey(x => x.Id)`.
- **`builder.Property(x => x.Id).ValueGeneratedNever()`** — the domain mints identities, so the database must not (`Rules.EntityKeysAreNeverDatabaseGenerated`).
- Each value object through a converter and comparer (§3).
- `builder.Property(x => x.CreatedAt).IsRequired()` — `CreatedAt` is on `Entity<TId>`, so every entity has one.
- **`builder.Ignore(x => x.DomainEvents)`**, on the aggregate root only (`Rules.DomainEventsAreNeverMapped`). Without it EF's relationship convention invents a `DomainEvents` table with a foreign key. A child entity has no such collection and needs no `Ignore`.
- Say nothing about the child collection: `<Child>.<Aggregate>Id` is discovered as the foreign key and the private backing field is written under the default `PropertyAccessMode.PreferField`. Configure a navigation only to depart from that.

## 3. A new value object

Copy `src/Todo.Domain/TodoLists/ValueObjects/TodoListTitle.cs`.

- `public sealed class <Name> : ValueObject`, in a `ValueObjects` namespace — `Rules.ValueObjectsDeriveFromValueObject` fails on a type there that does not derive.
- `public const int MinLength` / `MaxLength` where there is a bound; validators reference the constant, never a literal.
- Private constructor; `public static Result<<Name>> Create(...)` returning `DomainError.Validation` failures; `protected override IEnumerable<object> GetEqualityComponents()` yielding each component.
- Constructor parameters are **scalars only**. A value object nested inside another cannot bind.

**Its mapping is two things, not one**, and `Rules.ValueObjectsHaveAConverterAndComparer` checks for both. In the owning entity's configuration, in a `private static void Configure<Name>(EntityTypeBuilder<T> builder)`:

1. `.HasConversion(vo => vo.Value, value => <Name>.Create(value).Value)` — reading goes back through the domain factory, so a row that violates the domain fails loudly instead of becoming an invalid aggregate.
2. `.HasColumnName(...)`, `.HasMaxLength(<Name>.MaxLength)`, `.IsRequired()`. Value objects are **required, never optional** — EF 10's nullable complex-type support has open defects.
3. `property.Metadata.SetValueComparer(new ValueComparer<<Name>>((l, r) => l == r, vo => vo.GetHashCode(), vo => vo))`. **Not optional** — see the gotcha in `docs/gotchas.md`. `TodoListConfiguration.ConfigureTitle` is the worked example, and so is the reason not to reach for `ComplexProperty`.

A new value object adds a column, so it needs a migration.

## 4. A new child entity

Copy `src/Todo.Domain/TodoLists/Entities/TodoItem.cs`.

- `public sealed class <Child> : Entity<Guid>` — `Entity`, not `AggregateRoot`. Events belong to the root.
- It carries `public Guid <Aggregate>Id { get; private set; }`, set from the private constructor.
- **`Create` and every mutator are `internal`**, so the compiler — not a convention — makes the aggregate root the only way in. The root's method calls them, adds to the backing list, and raises any event itself.
- Its own configuration, table and key, `ValueGeneratedNever()` on the id, and no `Ignore` for domain events — it has none.
- New table, so it needs a migration.

## 5. A new domain event and its handler

An event is **two files plus a log line**, none of them optional.

1. `src/Todo.Domain/<Feature>/Events/<Name>Event.cs` — `public sealed record <Name>Event(Guid <Aggregate>Id, ...) : DomainEvent(<Aggregate>Id);`, carrying primitives rather than the aggregate.
2. `src/Todo.Application/<Feature>/Events/<Name>EventHandler.cs` — `internal sealed class <Name>EventHandler(ILogger<<Name>EventHandler> logger) : INotificationHandler<<Name>Event>`, returning `ValueTask.CompletedTask` when it has nothing to await.
3. A `[LoggerMessage]` entry in `src/Todo.Application/<Feature>/Events/<Aggregate>EventLog.cs`, an `internal static partial class`. **`EventId` is a sequence**: `1000`–`1002` are the pipeline behaviours in `Common/Behaviours/BehaviourLog.cs`, `2000`–`2002` are `TodoListEventLog`. Continue that block for a TodoLists event (next is `2003`); a **new feature starts at the next free thousand**, so a second feature's log begins at `3000`. The id is not decoration — it is what §11's dispatch test matches on, so give every event a distinct one and never renumber an existing one. `Rules.LoggedEventIdsAreUnique` enforces distinctness; which block you take is convention.

Without the handler the build fails with `error MSG0005: MediatorGenerator found message without any registered handler: <YourEvent>`, so **do not raise an event you have no reason to handle** — the handler costs a file whether or not it does anything.

Handlers run *inside* the unit of work, immediately before the save, so a handler's changes to a tracked aggregate join the same transaction, and events a handler raises are picked up on the next pass.

## 6. The request and its handler — one file

Open `CreateTodoListCommand.cs` (create), `AddTodoItemCommand.cs` (load, change a child, project), `ArchiveTodoListCommand.cs` (load, transition, return nothing) or `GetTodoListQuery.cs` (read) and follow it. What those files do not tell you:

- **`...Command` / `...Query`; handler `...Handler`, never `...CommandHandler`** — `Rules.RequestHandlersAreNamedHandler` turns the architecture suite red otherwise.
- **`ValueTask`, not `Task`.** This is `martinothamar/Mediator`, source-generated, not MediatR.
- **The record is `public`, the handler is `internal`** (`Rules.RequestsArePublicAndHandlersAreInternal`). The integration tests construct the request to post it; nothing outside the assembly constructs a handler.
- **The response is `Result` or `Result<T>` — always** (`Rules.RequestsRespondWithResult`). `ValidationBehaviour` is constrained `where TResponse : Result`, so a request answering with a bare DTO is silently never validated. That is the dead-validation defect this template exists to prevent.
- **`.ConfigureAwait(false)` on every await**, though no analyser requires it.
- Constructor-inject the repository and, when the use case writes, `IUnitOfWork`. Nothing else — no `DbContext`, no `ILogger`; logging is a behaviour.

- **A missing aggregate is the handler's own error**, not the repository's: a `private static DomainError <Aggregate>NotFound(Guid id)` beside the guard, carrying the one `NotFound` code every route for that aggregate shares. Repeating that helper in each of the aggregate's handlers is correct and is **not** a duplicate code — `ErrorCodeUniquenessTests` scans `Todo.Domain` only, so uniqueness is a rule about guards in the domain, not about Application.
- **A failed `Result` from the aggregate is passed straight out, uninspected.** A handler reading `.Error.Code` has taken a decision belonging to the domain.
- **Saving is what dispatches domain events** — one `SaveChangesAsync` per handler, at the end. Never two.

## 7. The validator — its own file, `public`

`public sealed class <UseCase>CommandValidator : AbstractValidator<<UseCase>Command>`, beside the command. Write one for every request (`Rules.EveryRequestHasAValidator`); a request with no fields is the only exception, and **queries get one too** — an all-zero `Guid` is a malformed request, not a request for something absent, so 400 and not 404 (`GetTodoListQueryValidator` is the one-line example).

- **`public`, and only `public`** (`Rules.ValidatorsArePublic`). `AddValidatorsFromAssemblyContaining<CreateTodoListCommandValidator>()` scans with `includeInternalTypes` at its default of `false`, so an `internal` validator compiles, registers nothing and never runs — and the endpoint then returns the domain's error where a shape failure belonged. Being `public` in this assembly *is* the registration; there is no list.
- **Shape only**: present, non-empty identifier, within `<ValueObject>.MaxLength` referenced as the constant. The aggregate owns invariants — minimum lengths, trimming, whether an archived list accepts changes.

Never restate a domain rule in a validator: the integration tests deliberately assert that a well-shaped-but-invalid input reaches the domain and returns the *domain's* code, so a duplicated rule turns that test red.

## 8. The repository

Declare the interface in Application, implement it in Infrastructure — `Rules.ApplicationUsesNoOrmTypes` means declaring a method here is the only way to get data.

- **Whole aggregates only.** When the aggregate has child collections, `GetByIdAsync` `Include`s every one of them, because an aggregate that arrives partial enforces its invariants against a collection it merely believes is empty. An aggregate with no children needs no `Include`.
- **Tracked**, no `AsNoTracking`. **Nothing here saves** — committing is `IUnitOfWork`'s. **One repository per aggregate root.**
- The implementation is `internal sealed class <Aggregate>Repository(TodoDbContext context)` using `_context.Set<<Aggregate>>()`.
- **A new aggregate's repository must be registered by hand** (`Rules.RepositoriesAreRegistered`): one `services.AddScoped<I<Aggregate>Repository, <Aggregate>Repository>();` in `src/Todo.Infrastructure/ConfigureServices.cs`. It is the only DI line a feature adds — handlers, validators, configurations and endpoints are all discovered.

## 9. The DTO

Only when no existing DTO fits — a rename answers with `TodoListDto`, not a `RenamedTodoListDto`. Copy `Dtos/TodoListDto.cs`: a record with an explicit `static FromDomain(...)` that flattens value objects to their primitive.

## 10. The endpoint

Derive from the feature's base class and implement `MapEndpoint`. **No registration anywhere** — `AddEndpoints` scans the assembly for `IEndpoint`, so adding an endpoint is adding a file. `AddTodoItemEndpoint.cs` is the fullest example.

Every endpoint derives from that base (`Rules.EndpointsDeriveFromTheirFeatureBase`), so a **new feature needs its base class first**: `internal abstract class <Aggregate>Endpoint : IEndpoint` — singular, like `TodoListEndpoint` — with `GroupPrefix => "/api/<kebab-plural>"` and `GroupTag => "<Feature>"`. Endpoints sharing a prefix are mapped into one route group, so both are stated once and cannot drift.

- **Bind, send, translate. Nothing else** — no validation, no branching on the result, no logic. `ArgumentNullException.ThrowIfNull(group)` is the first line of every `MapEndpoint`; CA1062 is off but every endpoint does it.
- **Never name an HTTP status code outside `src/Todo.Api/Common/ResultExtensions.cs`.** `DomainErrorType` drives the status through the single `StatusCodeFor` ladder; the endpoint chooses only the *shape of success* — `ToOk()`, `ToCreated(location)` or `ToNoContent()`. Copying an error-to-status ladder into an endpoint is the specific defect this template exists to avoid.
- **`Produces` / `ProducesProblem` are the one exception** — OpenAPI metadata, not a decision. List every status the slice can return, the domain's included.
- **When the route carries part of the command**, declare an `internal sealed record <Name>Request(...)` for the remaining body in the same file and assemble the command in the lambda; when the body *is* the whole command, bind the command directly.
- **Route shape.** A named transition the domain may refuse is its own sub-resource reached with `POST` — `POST /{id}/archive` — never a `PATCH` setting a field.

## 11. Tests

Two seams, as `docs/architecture.md` sets out, and **no handler tests with mocked repositories**.

**HTTP seam** — `tests/Todo.IntegrationTests/<Feature>/`, one file per feature: `public sealed class <Thing>EndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)`. The base class supplies `Client` and resets the database before each test; nothing needs registering, because `IntegrationTestBase` already carries the collection attribute. Cover at minimum:

1. The happy path, **read back through `GET`** — a response asserting about itself does not prove anything was persisted.
2. Each shape failure — 400 carrying `Validation.Failed` and the field name.
3. Each domain failure — the status *and* the stable code, `Assert.Contains("TodoList.X", body, StringComparison.Ordinal)`. Never assert on a message.
4. The unknown-identifier case — 404 carrying `<Aggregate>.NotFound`.
5. **Each domain event, proved to have been dispatched.** Nothing else can see this from out here.

Points 2 and 3 are the pair proving the two-layer split is real: an empty title is refused by the validator as `Validation.Failed`, while a well-shaped `"ab"` gets past it and returns the domain's `TodoList.Title.Length`. Write both and they tell you at once if the validator was never registered.

Point 5 needs the fixture itself, which the primary constructor above passes to the base and does not keep — add `private readonly TodoApiFixture _fixture = fixture;`. `_fixture.Logs` is a `LogCapture` that records nothing until a test opens it, so wrap only the request under test:

```csharp
private const int LabelCreatedEventId = 3000;   // the id its [LoggerMessage] declares

using (_fixture.Logs.Record())
{
    // the one request that should raise the event
}

Assert.Contains(_fixture.Logs.Records, record => record.EventId.Id == LabelCreatedEventId);
```

Match on the **`EventId`, never the message text**, so rewording a log line does not break the test. `ArchiveTodoListEndpointTests` extends the same capture to assert the *ordering* — the handler logs before EF's single `INSERT`/`UPDATE`, which is how "dispatched inside the same transaction" is observable from the HTTP seam. Copy it when an event handler writes anything.

Match the local conventions: request URIs are `Uri` objects from a private `static Uri XUri(Guid id)` helper, `Assert.Contains` passes `StringComparison.Ordinal`, and set-up goes through the API's own endpoints via private `CreateTodoListAsync`-style helpers rather than touching the database.

**Aggregate seam** — `tests/Todo.UnitTests/<Feature>/`, for invariant *matrices*: many cases, no I/O, usually a `[Theory]` over inputs and expected error codes. A rule with one or two outcomes needs nothing here; the HTTP seam already proves it end to end.

## 12. Build and run the suite

`dotnet build` then all three suites — the commands are in `docs/conventions.md`. 0 warnings, or you are not done.

## 13. What a feature does not touch

Editing one of these means checking you have not gone off the path:

- **`src/Todo.Application/ConfigureServices.cs`** — handlers and validators are found automatically. The *only* reason to edit it is a new **pipeline behaviour**, which is not auto-discovered: it must be added to `options.PipelineBehaviors`, outermost first, or it is written, registered and silently never runs.
- **`TodoDbContext.cs`**, **`EndpointExtensions.cs`**, **`Program.cs`** — configurations and endpoints arrive by assembly scan.
- **`ResultExtensions.cs`** — edited only for a new *success shape*, or when a new `DomainErrorType` makes its switch incomplete.
- **`AGENTS.md` and `CLAUDE.md`** — maps, regenerated by the documentation agent on the pull request. Change the code and let the map follow. `docs/` is the opposite: rules, human-owned, and the agent never writes there. See the `git-hygiene` skill.
- **Any existing migration** — the `add-migration` skill.

`src/Todo.Infrastructure/ConfigureServices.cs` is the one exception: a new aggregate's repository is registered there by hand.

## 14. Failure modes

| Symptom | Cause | Fix |
|---|---|---|
| `MSG0005` at build | A domain event with no `INotificationHandler` | Add the handler under `<Feature>/Events/`, or stop raising the event |
| `No suitable constructor was found for the type 'X'` at model build | A value object mapped as a `ComplexProperty`, or a constructor parameter with no scalar property to bind | Map it with a converter and comparer (§3) |
| An event handler never runs, though it exists | The aggregate root is not `AggregateRoot<Guid>` | `UnitOfWork` only enumerates that closed generic |
| A `DomainEvents` table appears in a migration | An aggregate configuration is missing `builder.Ignore(x => x.DomainEvents)` | Add it and regenerate |
| A value object's change is not written, or a write happens with nothing changed | The `ValueComparer` is missing | `SetValueComparer` on the property metadata |
| `ARCHITECTURE RULE BROKEN — Every IRequestHandler implementation is named *Handler` | Handler named `...CommandHandler` | Rename to `...Handler` |
| `ARCHITECTURE RULE NO LONGER DORMANT` | Your slice populated a layer a rule declared empty | Promote it to `Live`, or to `Thin` if that is still too few to be evidence. Never narrow the selector |
| `ARCHITECTURE RULE NO LONGER THIN` | Your slice grew a population past the point where a rule becomes meaningful — a second feature or a second aggregate root usually | Promote it to `Live` in `Rules.cs`. **Never raise `MeaningfulAt` to keep it `Thin`**; that is the ratchet running backwards |
| `ARCHITECTURE RULE THINNER THAN DECLARED` | A `Live` rule now examines fewer types than it claims to need — something was deleted, or a selector narrowed | Fix the selector, or declare the rule `Thin` |
| `ARCHITECTURE RULE VACUOUS` | A rule examined nothing at all | Fix the selector, or declare the rule `Dormant` |
| `DUPLICATE DOMAIN ERROR CODE` | Two guards raise the same code literal | Give the new guard its own code, or share one private helper |
| Malformed input returns the *domain's* error, or 404, instead of 400 `Validation.Failed` | The validator is `internal`, so the assembly scan skipped it | Make it `public` |
| The validator is public and still never runs | The request does not answer with a `Result`, so `ValidationBehaviour`'s constraint excludes it | `IRequest<Result>` or `IRequest<Result<T>>` |
| The endpoint 404s and its code never runs | The class is abstract, generic, or does not derive from the feature's endpoint base | Derive from it; the scan skips abstract and generic types |
| At the first request: `Unable to resolve service for type 'I<Aggregate>Repository' while attempting to activate '<UseCase>Handler'` | The repository was never registered. The build stays green — this is the one wiring step nothing discovers | One `AddScoped` line in `Todo.Infrastructure/ConfigureServices.cs` |
| A conflict comes back as 400, or a 500 appears | A status was decided in the endpoint, or the domain threw instead of returning a failed `Result` | Return `Result.Failure` with the right `DomainErrorType`; delete the endpoint's mapping |
| Two writes in one request, or an event handler's change not committed | `SaveChangesAsync` called twice, or an aggregate loaded through a second scope | One `SaveChangesAsync` per handler, at the end |
