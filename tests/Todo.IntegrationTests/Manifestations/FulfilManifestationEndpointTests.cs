using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Fulfilling a Manifestation - the one endpoint in this solution that cannot succeed.
/// </summary>
/// <remarks>
/// <para>
/// This is what the whole slice exists to prove. <c>ResultExtensions</c> claims a single ladder
/// turns an error category into a status, and until now every category in that ladder was written
/// at the same time as the ladder. <c>NotImplemented</c> is the first one added afterwards, and
/// these tests are the first evidence that adding one reaches a caller correctly rather than
/// falling through to a 500.
/// </para>
/// <para>
/// A 500 here would mean the gateway threw instead of returning, and
/// <c>UnhandledExceptionBehaviour</c> caught it - which is the specific failure the
/// <c>NotImplemented</c> category exists to prevent, and is why the status is asserted alongside
/// the code rather than instead of it.
/// </para>
/// </remarks>
public sealed class FulfilManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// The event id <c>ManifestationEventLog.ManifestationFailed</c> declares.
    /// </summary>
    private const int ManifestationFailedEventId = 3002;

    /// <summary>
    /// The event id <c>ManifestationEventLog.ManifestationRealized</c> declares. Asserted
    /// <em>absent</em>: nothing over HTTP may reach the realized path.
    /// </summary>
    private const int ManifestationRealizedEventId = 3001;

    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    private readonly TodoApiFixture _fixture = fixture;

    /// <summary>
    /// 501, and the RFC 9457 body carrying the stable code a caller branches on.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_ReturnsNotImplementedCarryingTheErrorCode()
    {
        var manifestation = await RequestManifestationAsync();

        var response = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // The code, not the prose. It arrives as the problem details title, which is how every
        // other failure in this API carries its code.
        Assert.Contains("reality.not-implemented", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The refusal is recorded rather than swallowed: the attempt happened, it did not work, and the
    /// Manifestation says so when read back.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_SettlesItAsFailed()
    {
        var manifestation = await RequestManifestationAsync();

        await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        var retrieved = await GetManifestationAsync(manifestation.Id);

        Assert.Equal("Failed", retrieved.State);
    }

    /// <summary>
    /// Terminal states are final, reached the only way HTTP can reach one. The second attempt still
    /// goes to the gateway - the handler cannot know which transition to ask for until reality
    /// answers - and the aggregate then refuses to settle twice, so the caller gets the aggregate's
    /// 409 rather than the gateway's 501. That precedence is the assertion here.
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

        Assert.Contains("Manifestation.AlreadySettled", body, StringComparison.Ordinal);

        // And the refused second attempt changed nothing.
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

    [Fact]
    public async Task Fulfil_WithAnEmptyIdentifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(FulfilUri(Guid.Empty), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Manifestation Id", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The failed domain event is dispatched, and the realized one is not - which is the assertion
    /// that the realized path is unreachable over HTTP rather than merely untested.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_DispatchesTheFailedEventAndNotTheRealizedOne()
    {
        var manifestation = await RequestManifestationAsync();

        using (_fixture.Logs.Record())
        {
            var response = await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        var records = _fixture.Logs.Records;

        Assert.Contains(records, record => record.EventId.Id == ManifestationFailedEventId);
        Assert.DoesNotContain(records, record => record.EventId.Id == ManifestationRealizedEventId);
    }

    /// <summary>
    /// Fulfilment leaves the TodoItem alone. Completion follows a <em>realized</em> Manifestation,
    /// and nothing here was realized - so a TodoItem quietly completing would mean the cross-aggregate
    /// reaction had been wired to the wrong event.
    /// </summary>
    [Fact]
    public async Task Fulfil_WithARequestedManifestation_LeavesTheTodoItemIncomplete()
    {
        var manifestation = await RequestManifestationAsync();

        await Client.PostAsync(FulfilUri(manifestation.Id), content: null);

        var response = await Client.GetAsync(TodoListUri(manifestation.TodoListId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var todoList = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(todoList);
        Assert.All(todoList.Items, item => Assert.False(item.IsComplete));
    }

    private static Uri FulfilUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}/fulfil", UriKind.Relative);

    private static Uri ManifestationUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}", UriKind.Relative);

    private static Uri TodoListUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}", UriKind.Relative);

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri ManifestUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/manifest", UriKind.Relative);

    private async Task<ManifestationDto> GetManifestationAsync(Guid manifestationId)
    {
        var response = await Client.GetAsync(ManifestationUri(manifestationId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(retrieved);

        return retrieved;
    }

    private async Task<ManifestationDto> RequestManifestationAsync()
    {
        var listResponse = await Client.PostAsJsonAsync(
            _todoLists,
            new CreateTodoListCommand("Weekend shopping"));

        Assert.Equal(HttpStatusCode.Created, listResponse.StatusCode);

        var todoList = await listResponse.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(todoList);

        var itemResponse = await Client.PostAsJsonAsync(
            ItemsUri(todoList.Id),
            new { Description = "Buy milk" });

        Assert.Equal(HttpStatusCode.Created, itemResponse.StatusCode);

        var todoItem = await itemResponse.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.NotNull(todoItem);

        var response = await Client.PostAsync(ManifestUri(todoList.Id, todoItem.Id), content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(created);

        return created;
    }
}
