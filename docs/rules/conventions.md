# Conventions

Naming, cross-cutting shape, and the stack constraints that are not visible from the build files.
**Where a file goes belongs to the `new-feature` skill**, which owns the order as well as the
placement.

## The stack

Versions live in `Directory.Packages.props` and shared compiler properties in
`Directory.Build.props`; read them there. What those files do not tell you:

- **.NET 10** (`net10.0`), SDK 10.0.302, and **deliberately no `global.json`**.
- The solution file is **`Todo.slnx`** — the .NET 10 XML format, **not** `.sln`. Tooling that
  assumes `.sln` will not find it.
- Mediator is `martinothamar/Mediator` — **source-generated CQRS, not MediatR**, and the two
  differ in more than performance. The generator lives in `Todo.Application`, so handlers must
  live in that assembly.
- **EF Core 10 is LTS to Nov 2028; EF 11 is STS. Stay on 10.**
- Central package management is on: `dotnet add <project> package <name>` writes the version to
  `Directory.Packages.props` and a versionless reference to the csproj. **Never put a `Version`
  attribute on a `PackageReference`.**
- **Node** is required — the Claude Code hooks are Node scripts.

## Commands

```bash
dotnet build                              # 0 warnings is the contract, not an aspiration
dotnet test                               # everything; integration needs Docker
dotnet test tests/Todo.ArchitectureTests  # layering only, no container, <1s
dotnet test tests/Todo.UnitTests          # domain only, no I/O
dotnet test tests/Todo.IntegrationTests   # real SQL Server in a container
```

`TreatWarningsAsErrors` is on solution-wide, but `AnalysisLevel` is `latest-recommended`, which
leaves **CA1062** (null-check public arguments), **CA2007** (`ConfigureAwait`) and **CA2234** (pass
`Uri`, not `string`) off. The solution follows all three by convention anyway, so a green build is
necessary and never sufficient.

## Code

- File-scoped namespaces. Private fields `_camelCase`. No `this.` qualification. 4-space indent.
  **Namespaces follow folders** (`Rules.NamespacesFollowFolders`), so moving a file changes its
  namespace and every `using` that named it.
- The Domain namespace is `Todo.Domain.TodoLists` — **plural**, because the singular collides with
  the `TodoList` class. Name every feature namespace so it cannot collide with its own aggregate.
- `ConfigureServices.cs` at each composable layer root exposing `Add<Layer>Services(...)`. The
  class is `ConfigureServices`, **not** `DependencyInjection`.
- Mediator: request record and handler in the **same file**, named for the request.
  `...Command` / `...Query`; handler `...Handler`, **never** `...CommandHandler`
  (`Rules.RequestHandlersAreNamedHandler`). Handlers return **`ValueTask`**. The record is public
  and the handler internal (`Rules.RequestsArePublicAndHandlersAreInternal`).
- Failures are `Result` / `Result<T>` carrying `DomainError(Code, Message, DomainErrorType)`, and
  `DomainErrorType` drives the one HTTP status translation. The type is `DomainError` and not
  `Error` because `Error` collides with a Visual Basic keyword and trips CA1716 — renaming was
  preferred to suppressing, and there are now **no `SuppressMessage` attributes anywhere** in
  `src` or `tests` — `Rules.NoSuppressMessageAnywhere`. Keep it that way.
- **Errors are constructed inline, at the guard that raises them**, so the rule and its error read
  together. There is no central `*Errors` class. Callers and tests match on the **code**, never the
  message. `ErrorCodeUniquenessTests` scans `Todo.Domain`'s source and fails on a repeated code
  literal, so every new guard needs its own.
- Ids are `Guid.CreateVersion7()`, minted by the domain
  (`Rules.EntityKeysAreNeverDatabaseGenerated` keeps the database out of it).
- DTOs are records with an explicit `static FromDomain(...)`. **No AutoMapper or Mapster.**
- Test naming: `Method_Scenario_ExpectedResult`. The test tree mirrors the production tree.
