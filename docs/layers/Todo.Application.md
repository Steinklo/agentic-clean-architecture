# Todo.Application — the rules

Use cases. Orchestrates the domain and declares what it needs from the outside world, without
knowing how any of it is implemented.

**How to add a command, query, validator, DTO or domain-event handler: the `new-feature` skill.**

## What belongs

Commands, queries and their handlers; pipeline behaviours under `Common/Behaviours/`; the
repository and unit-of-work **interfaces** whose implementations live in Infrastructure; DTOs with
a hand-written `FromDomain`; and FluentValidation validators.

## What must not

- **References Domain and nothing else** — `Rules.ApplicationReferencesOnlyDomain`.
- **No ORM types** — `Rules.ApplicationUsesNoOrmTypes`. If you need data, declare a repository
  method.
- **Every `IRequestHandler` implementation is named `*Handler`** —
  `Rules.RequestHandlersAreNamedHandler`.
- **Requests are public, handlers internal** — `Rules.RequestsArePublicAndHandlersAreInternal`.
- **Each request in its own use-case folder** — `Rules.RequestsLiveInAUseCaseFolder`.
- **DTOs in the feature's `Dtos` folder**, not the use case that happened to need one first —
  `Rules.DtosLiveInTheirFeaturesDtosNamespace`.
- No HTTP concerns. Handlers return `Result`, never a status code.
- No domain rule restated in a validator. Validators check shape, the aggregate checks invariants,
  and the validation behaviour returns a `Validation`-categorised failure rather than throwing.

## What fails silently here

Four things here produce no error and no warning. Three now have a rule, and reading their names
tells you what breaks:

- `Rules.ValidatorsArePublic` — an `internal` validator is invisible to the assembly scan.
- `Rules.RequestsRespondWithResult` — a request answering with anything else is skipped by
  `ValidationBehaviour`, not rejected by it.
- `Rules.EveryRequestHasAValidator` — no validator means malformed input reaches the domain.

**The fourth has no rule and cannot have one: a behaviour missing from `options.PipelineBehaviors`
in `AddMediator`.** Registering it in DI is not enough — the list is the wiring, and a behaviour
absent from it is written, resolvable and never called. That is the dead-validation defect this
template exists to prevent, and nothing but reading that list catches it. Behaviours are listed
outermost first.

A pipeline behaviour here takes `MessageHandlerDelegate<TRequest, TResponse> next` and is
constrained `where TRequest : IMessage` — this is Mediator, not MediatR, so check its API rather
than assuming.

## Advice, not rules

Not enforced anywhere, because no test can express them. Weigh them; do not assume something
checks.

- **`GetByIdAsync` must `Include` every child collection.** An aggregate that arrives partial
  enforces its invariants against a collection it merely believes is empty — so a `TodoList`
  loaded without its items would archive happily. Nothing can check this statically: a missing
  `Include` is a correct-looking query.
- **Log event ids are allocated in blocks per feature**, continuing from the last. Uniqueness *is*
  enforced (`Rules.LoggedEventIdsAreUnique`); which block you take is convention.

## Tests

No handler tests with mocked repositories. Use cases are verified through the HTTP seam in
`tests/Todo.IntegrationTests`.
