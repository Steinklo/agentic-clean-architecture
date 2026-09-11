using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Fulfilling a Manifestation — the one route in this API that cannot succeed.
/// </summary>
/// <remarks>
/// <para>
/// This is what the <c>NotImplemented</c> category was added for, and this file is the only place
/// the claim is actually checked. <c>ResultExtensions</c> says in its own remarks that a category
/// added without an arm in its switch reaches callers as a silent 500 — a habit, not a guarantee.
/// The assertion below is the thing that would have caught the habit being forgotten.
/// </para>
/// <para>
/// Note what is <em>not</em> refused. The Manifestation was created, persisted, read back and
/// transitioned to <c>Failed</c> by the same domain rules as anything else; only the single
/// genuinely impossible operation declines.
/// </para>
/// </remarks>
public sealed class FulfilManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>The event id <c>ManifestationEventLog.ManifestationFailed</c> declares.</summary>
    private const int ManifestationFailedEventId = 3002;

    /// <summary>The event id <c>ManifestationEventLog.ManifestationRealized</c> declares.</summary>
    private const int ManifestationRealizedEventId = 3001;

    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    private readonly TodoApiFixture _fixture = fixture;

    /// <summary>
    /// 501, carrying the gateway's own code in the problem details — so a caller can tell "this is
    /// not built" from "this broke" without reading prose.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_ReturnsNotImplementedCarryingTheErrorCode()
    {
        var manifestation = await RequestManifestationAsync();

        var response = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // RFC 9457 problem details: the stable code is the title, the message is the detail.
        Assert.Contains("reality.not-implemented", body, StringComparison.Ordinal);
        Assert.Contains("\"status\":501", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The refusal is recorded rather than discarded: an adapter that declined is an outcome, and
    /// the Manifestation settles as Failed. This is also what makes the 409 below reachable.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_SettlesItAsFailed()
    {
        var manifestation = await RequestManifestationAsync();

        var response = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);

        var retrieved = await GetManifestationAsync(manifestation.Id);

        Assert.Equal("Failed", retrieved.State);
    }

    /// <summary>
    /// The terminal-state invariant, reached over HTTP. The domain refuses the second attempt as a
    /// conflict — the endpoint names no status, so the 409 is the category's and not the route's.
    /// </summary>
    [Fact]
    public async Task Fulfil_Twice_ReturnsConflictOnTheSecondAttempt()
    {
        var manifestation = await RequestManifestationAsync();

        var first = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        Assert.Equal(HttpStatusCode.NotImplemented, first.StatusCode);

        var second = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var body = await second.Content.ReadAsStringAsync();

        Assert.Contains("Manifestation.Terminal", body, StringComparison.Ordinal);

        // The refused second attempt changed nothing.
        var retrieved = await GetManifestationAsync(manifestation.Id);

        Assert.Equal("Failed", retrieved.State);
    }

    [Fact]
    public async Task Fulfil_WithAnUnknownManifestation_ReturnsNotFound()
    {
        var response = await Client.PostAsync(FulfilUri(Guid.CreateVersion7()), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Manifestation.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// A malformed identifier is 400 and never reaches the gateway. Worth its own test: a shape
    /// failure reported as 501 would tell the caller the feature is missing when the request was
    /// simply wrong.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithAnEmptyIdentifier_ReturnsBadRequestAndNot501()
    {
        var response = await Client.PostAsync(FulfilUri(Guid.Empty), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.DoesNotContain("reality.not-implemented", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The failed event is dispatched, and the realized one is not. The second half is the
    /// assertion that matters: no request can reach <c>Realize</c>, and this is what would notice if
    /// one ever could.
    /// </summary>
    [Fact]
    public async Task Fulfil_WhenRealityDeclines_DispatchesTheFailedEventAndNotTheRealizedOne()
    {
        var manifestation = await RequestManifestationAsync();

        using (_fixture.Logs.Record())
        {
            var response = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        var records = _fixture.Logs.Records.ToList();

        Assert.Contains(records, record => record.EventId.Id == ManifestationFailedEventId);
        Assert.DoesNotContain(records, record => record.EventId.Id == ManifestationRealizedEventId);
    }

    /// <summary>
    /// The TodoItem is untouched by a failed Manifestation. Manifesting is not a second route to
    /// completion, and the two aggregates stay independent when the reaction does not fire.
    /// </summary>
    [Fact]
    public async Task Fulfil_WhenRealityDeclines_LeavesTheTodoItemIncomplete()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var manifestation = await RequestManifestationAsync(todoList.Id, milk.Id);

        await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.All(retrieved.Items, item => Assert.False(item.IsComplete));
    }

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri TodoListUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}", UriKind.Relative);

    private static Uri ManifestUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/manifest", UriKind.Relative);

    private static Uri ManifestationUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}", UriKind.Relative);

    private static Uri FulfilUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}/fulfil", UriKind.Relative);

    private async Task<TodoListDto> CreateTodoListAsync(string title)
    {
        var response = await Client.PostAsJsonAsync(_todoLists, new CreateTodoListCommand(title));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(created);

        return created;
    }

    private async Task<TodoItemDto> AddTodoItemAsync(Guid todoListId, string description)
    {
        var response = await Client.PostAsJsonAsync(
            ItemsUri(todoListId),
            new { Description = description });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var added = await response.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.NotNull(added);

        return added;
    }

    /// <summary>Sets a Manifestation up end to end, through the API's own routes.</summary>
    private async Task<ManifestationDto> RequestManifestationAsync()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        return await RequestManifestationAsync(todoList.Id, milk.Id);
    }

    private async Task<ManifestationDto> RequestManifestationAsync(Guid todoListId, Guid todoItemId)
    {
        var response = await Client.PostAsync(ManifestUri(todoListId, todoItemId), content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(created);

        return created;
    }

    private async Task<ManifestationDto> GetManifestationAsync(Guid manifestationId)
    {
        var response = await Client.GetAsync(ManifestationUri(manifestationId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(retrieved);

        return retrieved;
    }

    private async Task<TodoListDto> GetTodoListAsync(Guid todoListId)
    {
        var response = await Client.GetAsync(TodoListUri(todoListId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(retrieved);

        return retrieved;
    }
}
