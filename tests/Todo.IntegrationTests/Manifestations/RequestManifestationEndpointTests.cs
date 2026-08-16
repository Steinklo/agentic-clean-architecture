using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Requesting a Manifestation, exercised over real HTTP against the real host and a real database.
/// </summary>
/// <remarks>
/// A Manifestation is the solution's second aggregate root, so this is also the first proof that
/// two aggregates persist independently: the Manifestation is written by its own repository, into
/// its own table, with no foreign key to the TodoList it names - and is read back through an
/// endpoint at its own root.
/// </remarks>
public sealed class RequestManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// The event id <c>ManifestationEventLog.ManifestationRequested</c> declares. Matched on rather
    /// than the message, so rewording the log line does not break this test.
    /// </summary>
    private const int ManifestationRequestedEventId = 3000;

    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    private readonly TodoApiFixture _fixture = fixture;

    [Fact]
    public async Task Post_ForATodoItem_ReturnsCreatedAtTheNewManifestation()
    {
        var (todoList, todoItem) = await CreateTodoListWithItemAsync();

        var response = await Client.PostAsync(ManifestUri(todoList.Id, todoItem.Id), content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(todoList.Id, created.TodoListId);
        Assert.Equal(todoItem.Id, created.TodoItemId);
        Assert.Equal("Requested", created.State);

        // The Location points at the Manifestation's own root, not back at the TodoList - unlike a
        // TodoItem, a Manifestation is an aggregate root and is retrievable on its own.
        Assert.Equal($"/api/manifestations/{created.Id}", response.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// The persisted fact, read back through a different endpoint than the one that wrote it -
    /// which is what makes this evidence of persistence rather than a response asserting about
    /// itself.
    /// </summary>
    [Fact]
    public async Task PostThenGet_ForATodoItem_RoundTripsEveryValueUnchanged()
    {
        var (todoList, todoItem) = await CreateTodoListWithItemAsync();

        var created = await RequestManifestationAsync(todoList.Id, todoItem.Id);

        var response = await Client.GetAsync(ManifestationUri(created.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(retrieved);
        Assert.Equal(created, retrieved);
        Assert.Equal("Requested", retrieved.State);
    }

    /// <summary>
    /// Two Manifestations of the same TodoItem are both accepted. There is deliberately no rule
    /// against it - nothing in the issue asks for one - and asserting it here means adding one later
    /// is a conscious act rather than a silent behaviour change.
    /// </summary>
    [Fact]
    public async Task Post_TwiceForTheSameTodoItem_CreatesTwoManifestations()
    {
        var (todoList, todoItem) = await CreateTodoListWithItemAsync();

        var first = await RequestManifestationAsync(todoList.Id, todoItem.Id);
        var second = await RequestManifestationAsync(todoList.Id, todoItem.Id);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Post_WithAnUnknownTodoList_ReturnsNotFound()
    {
        var response = await Client.PostAsync(
            ManifestUri(Guid.CreateVersion7(), Guid.CreateVersion7()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The TodoList exists and does not own that TodoItem. The Manifestation cannot check this - it
    /// holds an id and cannot reach the other aggregate - so the use case checks it, and answers
    /// with the same code the TodoList itself raises for an unknown item.
    /// </summary>
    [Fact]
    public async Task Post_WithATodoItemTheTodoListDoesNotOwn_ReturnsNotFound()
    {
        var (todoList, _) = await CreateTodoListWithItemAsync();

        var response = await Client.PostAsync(
            ManifestUri(todoList.Id, Guid.CreateVersion7()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Item.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shape layer: an all-zero identifier is malformed, not absent, so it is 400 and not 404.
    /// Present so the validator cannot be registered-but-never-run without a test noticing.
    /// </summary>
    [Fact]
    public async Task Post_WithAnEmptyIdentifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            ManifestUri(Guid.Empty, Guid.Empty),
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Todo List Id", body, StringComparison.Ordinal);
        Assert.Contains("Todo Item Id", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The requested domain event is dispatched, which nothing outside the host can otherwise see.
    /// </summary>
    [Fact]
    public async Task Post_ForATodoItem_DispatchesTheRequestedEvent()
    {
        var (todoList, todoItem) = await CreateTodoListWithItemAsync();

        using (_fixture.Logs.Record())
        {
            var response = await Client.PostAsync(ManifestUri(todoList.Id, todoItem.Id), content: null);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        Assert.Contains(
            _fixture.Logs.Records,
            record => record.EventId.Id == ManifestationRequestedEventId);
    }

    private static Uri ManifestUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/manifest", UriKind.Relative);

    private static Uri ManifestationUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}", UriKind.Relative);

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private async Task<(TodoListDto TodoList, TodoItemDto TodoItem)> CreateTodoListWithItemAsync()
    {
        var response = await Client.PostAsJsonAsync(
            _todoLists,
            new CreateTodoListCommand("Weekend shopping"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todoList = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(todoList);

        var added = await Client.PostAsJsonAsync(ItemsUri(todoList.Id), new { Description = "Buy milk" });

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);

        var todoItem = await added.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.NotNull(todoItem);

        return (todoList, todoItem);
    }

    private async Task<ManifestationDto> RequestManifestationAsync(Guid todoListId, Guid todoItemId)
    {
        var response = await Client.PostAsync(ManifestUri(todoListId, todoItemId), content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(created);

        return created;
    }
}
