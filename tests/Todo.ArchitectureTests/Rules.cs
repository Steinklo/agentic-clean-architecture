namespace Todo.ArchitectureTests;

/// <summary>
/// How much a rule is currently proving.
/// </summary>
/// <remarks>
/// This exists because of the one failure mode that makes an architecture suite actively harmful.
/// NetArchTest — and any rule written over a set of types — reports <em>success</em> when the set
/// it examined was empty. "Every request handler is named <c>*Handler</c>" therefore passes today
/// purely because no handler exists yet, and would keep passing if the rule itself were broken.
/// A rule that cannot fail is worse than no rule, because the green tick reads as coverage.
/// <para>
/// The same trap has a quieter second form. A rule can examine a handful of types and still prove
/// nothing, because those types were written in the same change as the rule: "every feature places
/// its commands in a use-case folder" is not evidence of a convention when there is one feature.
/// It passes by construction. <see cref="Thin"/> is the name for that, so a rule cannot borrow
/// credibility from a population it has not got.
/// </para>
/// <para>
/// So every rule declares its state here, along with the population it becomes meaningful at, and
/// <see cref="Rule"/> checks both against the size of the set actually examined. Every direction
/// fails loudly, which is what keeps these declarations honest as the solution fills in.
/// </para>
/// </remarks>
internal enum RuleCoverage
{
    /// <summary>
    /// The rule examines enough types to mean what it says — at least its
    /// <see cref="ArchitectureRule.MeaningfulAt"/>. Examining fewer fails the suite rather than
    /// passing: the selector has broken, the code it guards has been deleted, or the rule was
    /// never as broad as it claimed.
    /// </summary>
    Live,

    /// <summary>
    /// The rule examines something, but too little to prove the convention rather than to restate
    /// one example. A rule over "every feature" when there is exactly one feature passes because
    /// that feature was written alongside it — which is not evidence.
    /// <para>
    /// It still runs, and still fails on a violation, so it is not decorative. What it does not
    /// do is imply breadth it has not got, and <see cref="Rules"/> — the file people read to learn
    /// what the suite guarantees — says so out loud. The moment it reaches its
    /// <see cref="ArchitectureRule.MeaningfulAt"/>, the suite fails and demands promotion to
    /// <see cref="Live"/>.
    /// </para>
    /// </summary>
    Thin,

    /// <summary>
    /// There is nothing for the rule to examine yet, because the layer it guards is still empty.
    /// The rule is declared, is not enforcing anything, and says so. The moment it has something
    /// to examine, the suite fails and demands it be promoted — so dormancy is a ratchet that
    /// cannot quietly outlive its reason.
    /// </summary>
    Dormant
}

/// <summary>
/// One architecture rule: the sentence it enforces, and how much it is proving today.
/// </summary>
/// <remarks>
/// All three coverage states are checked in both directions, so every declaration here has to
/// stay true as the solution fills in. There is no state that means "do not check me".
/// </remarks>
/// <param name="Name">The rule as a sentence. It leads every failure message.</param>
/// <param name="Coverage">How much the rule has to examine today.</param>
/// <param name="MeaningfulAt">
/// How many examined types it takes for this rule to be evidence of a convention rather than a
/// description of one example. Defaults to 1, which is right for a rule about a type that is
/// unique by construction — there is only one Domain project, so a rule over its references means
/// the same whether it reads one file or ten. Set it higher for a rule quantified over a
/// population that is expected to grow, such as "every feature" or "every aggregate root": below
/// this many, declare the rule <see cref="RuleCoverage.Thin"/> and let the ratchet promote it.
/// </param>
internal sealed record ArchitectureRule(string Name, RuleCoverage Coverage, int MeaningfulAt = 1);

/// <summary>
/// Every architecture rule in the solution, and its current coverage, in one place.
/// </summary>
/// <remarks>
/// This is the file to read to find out what the suite actually guarantees today.
/// <list type="bullet">
///   <item><description><b>Live</b> — proven against enough real types to mean what it says.</description></item>
///   <item><description><b>Thin</b> — it runs and it bites, but over too small a population to be
///   evidence of a convention rather than a description of the one example that exists. Read a
///   green tick here as "nothing contradicts this yet".</description></item>
///   <item><description><b>Dormant</b> — declared, but the layer it guards is still empty, so the
///   green tick means nothing yet. Populating that layer turns the suite red until the rule is
///   promoted.</description></item>
/// </list>
/// <para>
/// Every rule also declares the population at which it becomes meaningful, and the suite fails in
/// both directions against that number — so neither a shrinking codebase nor a growing one can
/// leave a declaration here quietly wrong.
/// </para>
/// </remarks>
internal static class Rules
{
    /// <summary>
    /// Live: read straight out of <c>Todo.Domain.csproj</c>, which always exists.
    /// Its passing state is an empty result, so it is additionally guarded by
    /// <see cref="LayerDependencyTests.ProjectReferences_GivenAProjectFileWithReferences_FindsEveryOne"/>.
    /// </summary>
    public static ArchitectureRule DomainHasNoProjectReferences { get; } =
        new("Domain references no other project", RuleCoverage.Live);

    /// <summary>Live: read straight out of <c>Todo.Application.csproj</c>, which always exists.</summary>
    public static ArchitectureRule ApplicationReferencesOnlyDomain { get; } =
        new("Application references Todo.Domain and nothing else", RuleCoverage.Live);

    /// <summary>
    /// Live: Domain is fully implemented. Meaningful at 5 types — a negative rule over a whole
    /// layer only says something while the layer has content, and Domain dropping below a handful
    /// of types means the selector broke, not that the rule got easier.
    /// </summary>
    public static ArchitectureRule DomainUsesNoOrmTypes { get; } =
        new("No object-relational mapping type appears in Todo.Domain", RuleCoverage.Live, MeaningfulAt: 5);

    /// <summary>
    /// Live: promoted by ticket 05, which gave Todo.Application its first types - the request
    /// pipeline, its behaviours, and the TodoLists feature.
    /// </summary>
    public static ArchitectureRule ApplicationUsesNoOrmTypes { get; } =
        new("No object-relational mapping type appears in Todo.Application", RuleCoverage.Live, MeaningfulAt: 5);

    /// <summary>
    /// Live: Todo.Api's entry point already carries the whole of <c>Program.cs</c>, so the rule
    /// bites on composition-root code today.
    /// </summary>
    public static ArchitectureRule ApiUsesNoOrmTypes { get; } =
        new("No object-relational mapping type appears in Todo.Api", RuleCoverage.Live, MeaningfulAt: 5);

    /// <summary>
    /// Live: promoted by ticket 05, whose CreateTodoListHandler and GetTodoListHandler are the
    /// first implementations of <c>IRequestHandler</c>. Meaningful at 3 — one handler proves a
    /// spelling, several prove a convention.
    /// </summary>
    public static ArchitectureRule RequestHandlersAreNamedHandler { get; } =
        new("Every IRequestHandler implementation is named *Handler", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live: TodoList and TodoItem both exist, and they are independent instances of the rule
    /// rather than one example counted twice — which is why 2 is enough here and is not enough
    /// for a rule quantified over features.
    /// </summary>
    public static ArchitectureRule EntitiesHaveNoPublicSetters { get; } =
        new("Entities expose no public property setter", RuleCoverage.Live, MeaningfulAt: 2);

    /// <summary>Live: TodoListTitle and TodoItemDescription both exist, independently.</summary>
    public static ArchitectureRule ValueObjectsDeriveFromValueObject { get; } =
        new("Every type in a ValueObjects namespace derives from ValueObject", RuleCoverage.Live, MeaningfulAt: 2);

    // ---------------------------------------------------------------------------------------
    // The request pipeline. Each of the next three guards a failure that compiles, registers,
    // runs green and does nothing -- which is the class of defect this template exists to
    // prevent, and the one no other seam catches.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Live over 5 requests. <c>ValidationBehaviour</c> is constrained <c>where TResponse :
    /// Result</c>, so a request answering with a bare DTO is silently skipped by validation --
    /// it compiles, its validator registers, and nothing ever calls it.
    /// </summary>
    public static ArchitectureRule RequestsRespondWithResult { get; } =
        new("Every IRequest answers with Result or Result<T>", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live over 5 validators. <c>AddValidatorsFromAssemblyContaining</c> scans with
    /// <c>includeInternalTypes</c> at its default of <see langword="false"/>, so an internal
    /// validator compiles, registers nothing and never runs. Being public in this assembly
    /// <em>is</em> the registration; there is no list to forget to add to.
    /// </summary>
    public static ArchitectureRule ValidatorsArePublic { get; } =
        new("Every AbstractValidator in Todo.Application is public", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live over 5 requests. A request with no validator reaches the domain with unchecked shape,
    /// so malformed input comes back as the domain's error, or as 404, where 400 belonged.
    /// </summary>
    public static ArchitectureRule EveryRequestHasAValidator { get; } =
        new("Every IRequest has an AbstractValidator", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live: promoted by the Manifestations slice, which gave the solution its second aggregate
    /// root. Two roots keyed independently is the point at which this stops describing the one
    /// example that existed. <c>UnitOfWork</c> enumerates
    /// <c>ChangeTracker.Entries&lt;AggregateRoot&lt;Guid&gt;&gt;()</c>, so a root keyed on
    /// anything else compiles, saves, and never dispatches a domain event.
    /// </summary>
    public static ArchitectureRule AggregateRootsAreKeyedOnGuid { get; } =
        new("Every aggregate root derives from AggregateRoot<Guid>", RuleCoverage.Live, MeaningfulAt: 2);

    // ---------------------------------------------------------------------------------------
    // The shape of a vertical slice. These were the new-feature skill's rules and nothing
    // else's, which made them advice an agent could drift from between one slice and the next.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Live over 5 requests. The integration tests construct a request to post it, so the record
    /// is public; nothing outside the assembly constructs a handler, so the handler is internal.
    /// </summary>
    public static ArchitectureRule RequestsArePublicAndHandlersAreInternal { get; } =
        new("Request records are public and their handlers are internal", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live over 5 use-case folders. A request loose in <c>Commands/</c> rather than in its own
    /// folder is the first step to a feature whose slices stop looking alike.
    /// </summary>
    public static ArchitectureRule RequestsLiveInAUseCaseFolder { get; } =
        new("Every request lives in <Feature>/Commands|Queries/<UseCase>", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live over 2 DTOs. They are shared by more than one use case — <c>TodoListDto</c> answers
    /// both CreateTodoList and GetTodoList — so they belong to the feature, not to either folder.
    /// </summary>
    public static ArchitectureRule DtosLiveInTheirFeaturesDtosNamespace { get; } =
        new("Every DTO lives in its feature's Dtos namespace", RuleCoverage.Live, MeaningfulAt: 2);

    /// <summary>
    /// Live over 5 endpoints. The route prefix and OpenAPI tag are stated once on the feature's
    /// base class, so a feature cannot end up half under one route and half under another.
    /// </summary>
    public static ArchitectureRule EndpointsDeriveFromTheirFeatureBase { get; } =
        new("Every endpoint derives from its feature's endpoint base", RuleCoverage.Live, MeaningfulAt: 3);

    /// <summary>
    /// Live: promoted by the Manifestations slice, whose <c>IManifestationRepository</c> is the
    /// second contract this has to find a registration for. This is the one wiring step nothing
    /// discovers — handlers, validators, configurations and endpoints are all found by scanning, so
    /// a missing <c>AddScoped</c> builds green and throws at the first request instead.
    /// <para>
    /// It sees repositories only, by name. <c>IRealityGateway</c> is a port registered on the same
    /// line of the same file and is invisible here, so a port that is not a repository still has
    /// nothing checking it was wired up.
    /// </para>
    /// </summary>
    public static ArchitectureRule RepositoriesAreRegistered { get; } =
        new("Every repository interface is registered in Infrastructure", RuleCoverage.Live, MeaningfulAt: 2);

    // ---------------------------------------------------------------------------------------
    // The mapping. Every one of these is a gotcha AGENTS.md records because it already cost
    // someone real time, and every one of them fails quietly: the model builds, the schema is
    // created, rows are written, and something is subtly wrong.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Live over 2 mapped entities. The domain mints identities with
    /// <c>Guid.CreateVersion7()</c>, so a key the database also generates means two sources of
    /// truth for identity and a value the aggregate never agreed to.
    /// </summary>
    public static ArchitectureRule EntityKeysAreNeverDatabaseGenerated { get; } =
        new("Every mapped entity key is ValueGeneratedNever", RuleCoverage.Live, MeaningfulAt: 2);

    /// <summary>
    /// Live: promoted by the Manifestations slice, which mapped the second aggregate root. Without
    /// <c>Ignore(x =&gt; x.DomainEvents)</c> EF's relationship convention maps the collection as a
    /// navigation, inventing a DomainEvents table with a foreign key — visible only when someone
    /// reads the next migration.
    /// </summary>
    public static ArchitectureRule DomainEventsAreNeverMapped { get; } =
        new("No aggregate root maps its DomainEvents collection", RuleCoverage.Live, MeaningfulAt: 2);

    /// <summary>
    /// Live over 2 value-object properties. A value object needs a converter <em>and</em> an
    /// explicit comparer; with the converter alone EF compares by reference, so a
    /// changed value object can be silently not written, or a write happen with nothing changed.
    /// </summary>
    public static ArchitectureRule ValueObjectsHaveAConverterAndComparer { get; } =
        new("Every mapped value object has a ValueConverter and an explicit ValueComparer", RuleCoverage.Live, MeaningfulAt: 2);

    // ---------------------------------------------------------------------------------------
    // Layout and logging.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Live over every authored source file. Namespaces follow folders here, which is what lets
    /// the use-case-folder rule assert on a namespace and mean a directory. Moving a file changes
    /// its namespace, and a file whose two disagree breaks that equivalence silently.
    /// </summary>
    public static ArchitectureRule NamespacesFollowFolders { get; } =
        new("Every source file's namespace matches its folder path", RuleCoverage.Live, MeaningfulAt: 20);

    /// <summary>
    /// Live over 6 logged events. The integration tests prove a domain event was dispatched by
    /// matching on its EventId rather than its message text, so two events sharing an id makes
    /// that assertion pass for the wrong reason.
    /// </summary>
    public static ArchitectureRule LoggedEventIdsAreUnique { get; } =
        new("Every [LoggerMessage] EventId is unique", RuleCoverage.Live, MeaningfulAt: 4);
}
