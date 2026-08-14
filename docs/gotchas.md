# Gotchas

Each of these cost someone real time. Do not rediscover them.

Where a gotcha now has a rule, the rule is named — reading its name tells you what breaks, and the
suite catches it whether or not anyone read this file.

## Entity Framework

- **`ComplexProperty` cannot work here.** EF 10 cannot bind a complex-type instance to a
  constructor parameter ([efcore#31621](https://github.com/dotnet/efcore/issues/31621), open,
  absent from EF 10 and 11), and every entity here has a private constructor taking value objects —
  declaring one complex fails at model build with *"No suitable constructor was found"*. Use a
  `ValueConverter` **plus an explicit `ValueComparer`**; the comparer is not optional, and omitting
  it gives silently wrong change tracking (`Rules.ValueObjectsHaveAConverterAndComparer`).
- **The EF Core complex-types documentation on Microsoft Learn is written against EF 11.**
  Nested-property lambda config (`.Property(c => c.Address.Line1)`), `HasIndex` into complex types,
  and complex types on TPT/TPC **do not exist in EF 10**. Code copied from that page will not
  compile.
- **Never add a parameterless constructor beside a rich one.** EF binds the constructor with the
  fewest property parameters, so an empty one wins and your invariant-bearing constructor silently
  becomes dead code.
- **Value objects must be required, never optional.** EF 10's nullable complex-type support still
  has open defects upstream.
- **`Ignore` the `DomainEvents` collection in every aggregate-root configuration**, or EF's
  relationship convention maps it as a collection navigation with its own table and foreign key
  (`Rules.DomainEventsAreNeverMapped`).

## Mediator

- **Pipeline behaviours are not auto-discovered.** They must be listed, outermost first, in
  `options.PipelineBehaviors` inside `AddMediator`. A behaviour written, registered in DI and left
  off that list **silently never runs** — precisely the dead-validation defect this template exists
  to avoid. This is the one silent failure in the request pipeline with no rule behind it, because
  nothing but reading that list can see it.
- **Every notification type needs a handler**, or the generator emits `MSG0005`, which is an error
  here because warnings are errors. Raise a domain event and you must handle it.

## Build and tooling

- **`IDE0005` (unused usings) does nothing on build unless `GenerateDocumentationFile` is `true`** —
  a Roslyn limitation. That in turn enables `CS1591`, which is suppressed. Both lines are in
  `Directory.Build.props`; removing one without the other silently disables the check.
- **`SSH.NET` is pinned forward in `Directory.Packages.props`** to clear a high-severity advisory
  arriving transitively through Testcontainers; `TreatWarningsAsErrors` turns NuGet audit findings
  into build errors, which is intended. Drop the pin when Testcontainers ships a fixed dependency.

## Hosting and tests

- **`ConfigureAppConfiguration` does not reach the app under minimal hosting.**
  `WebApplicationBuilder` reads configuration eagerly in `Program.cs`, before
  `WebApplicationFactory`'s callbacks run. Use `UseSetting`, which writes into host configuration.
- **The integration test host runs as `Testing`, not `Development`**, because
  `appsettings.Development.json` carries a connection string that would otherwise beat the
  container override — failing with a connection timeout that looks nothing like a configuration
  problem.
- **Respawn refuses to build against a schema with no tables.** The fixture creates it lazily, so
  an empty database is simply nothing to reset.
- **SQL Server container images are x86-64 only.** Microsoft supports neither Rosetta 2, Prism nor
  QEMU at any version including 2025, and there is no ARM image, so integration tests cannot run
  natively on Apple Silicon.
- **Pin the Testcontainers image explicitly.** The parameterless `MsSqlBuilder()` has been
  deprecated since 4.10.
