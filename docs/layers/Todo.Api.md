# Todo.Api — the rules

The outermost layer. Turns HTTP into requests and `Result` into responses, and does nothing else.

**How to add an endpoint or a feature route group: the `new-feature` skill.**

## What belongs

Minimal API endpoints, discovered by an assembly scan for `IEndpoint` and grouped per feature — so
adding an endpoint is adding a file and there is no route list to keep in step. Composition in
`Program.cs`, and the single `Result` → `IResult` translation in `Common/ResultExtensions.cs`.

## What must not

- **No ORM types** — `Rules.ApiUsesNoOrmTypes`. The rule forbids the EF namespaces rather than the
  project reference, which is exactly why `AddInfrastructureServices(...)` is legal here and a
  stray `AddDbContext<T>()` in `Program.cs` is not.
- **Every endpoint derives from its feature's endpoint base** —
  `Rules.EndpointsDeriveFromTheirFeatureBase`. Implementing `IEndpoint` directly compiles and
  routes; it also restates the prefix and tag that the base exists to state once, which is how a
  feature ends up half under one route.
- No business logic, no validation rules, no direct database access.
- **No per-endpoint status mapping.** One translation, driven by `DomainErrorType`: `Validation` →
  400, `NotFound` → 404, `Conflict` → 409, `NotImplemented` → 501, `Failure` → 500. An endpoint
  picks only the shape of success and names a status code nowhere but its
  `Produces`/`ProducesProblem` metadata. Copying that ladder into an endpoint is the specific defect
  this template exists to avoid.
- **Adding a category to that ladder is not a compile error, and cannot be made into one.** The
  switch ends in a discard; removing it makes the switch itself fail CS8524 ("not exhaustive
  involving an unnamed enum value") because a C# enum can hold a value no member names, and
  `TreatWarningsAsErrors` stops the build. So a new `DomainErrorType` without an arm reaches callers
  as a 500 with nothing said. Add its arm in the same change.

## Configuration

`appsettings.Development.json` carries a connection string for running the API by hand against
docker compose; it is development-only and its credentials are not a pattern to copy. Migration on
startup is gated behind `Database:MigrateOnStartup` and off by default. The integration test host
deliberately runs as **`Testing`**, so that file does not apply there.

## Tests

Every endpoint is verified through the HTTP seam in `tests/Todo.IntegrationTests` — real requests,
real database. Cover the failure responses, not just the happy path.
