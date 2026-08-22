using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Requesting a Manifestation against a TodoItem, over real HTTP against a real database.
/// </summary>
/// <remarks>
/// The route is a sub-resource of the TodoItem, so this test also proves the endpoint reached its
/// intended prefix — a Manifestations endpoint deriving from the wrong feature base would 404 here
/// rather than fail an assertion.
/// </remarks>
public sealed class RequestManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>The event id <c>ManifestationEventLog.ManifestationRequested</c> declares.</summary>
    private const int ManifestationRequestedEventId = 3000;

    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    private readonly TodoApiFixture _fixture = fixture;

    [Fact]
    public async Task Request_ForAnExistingTodoItem_CreatesARequestedManifestation()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        var response = await Client.PostAsync(ManifestUri(todoList.Id, milk.Id), content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(created);
        Assert.Equal(todoList.Id, created.TodoListId);
        Assert.Equal(milk.Id, created.TodoItemId);
        Assert.Equal("Requested", created.State);

        // The Location header addresses the Manifestation at its own root, not under the TodoList.
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(
            $"/api/manifestations/{created.Id}",
            response.Headers.Location.ToString(),
            StringComparison.Ordinal);

        // Persisted, not merely asserted about itself: read it back through GET.
        var retrieved = await GetManifestationAsync(created.Id);

        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Requested", retrieved.State);
    }

    /// <summary>
    /// Two Manifestations against one TodoItem are both allowed. Nothing in the domain says
    /// otherwise, and asserting it here means a rule added later has to be added deliberately.
    /// </summary>
    [Fact]
    public async Task Request_Twice_ForTheSameTodoItem_CreatesTwoManifestations()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        var first = await RequestManifestationAsync(todoList.Id, milk.Id);
        var second = await RequestManifestationAsync(todoList.Id, milk.Id);

        Assert.NotEqual(first.Id, second.Id);
    }

    /// <summary>
    /// Requesting against a TodoItem that belongs to a different TodoList is a 404 and not a
    /// success: the item is looked for inside the aggregate named by the route, never globally.
    /// </summary>
    [Fact]
    public async Task Request_WithATodoItemFromAnotherTodoList_ReturnsNotFound()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var otherList = await CreateTodoListAsync("Chores");
        var otherItem = await AddTodoItemAsync(otherList.Id, "Wash up");

        var response = await Client.PostAsync(ManifestUri(todoList.Id, otherItem.Id), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Item.NotFound", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_WithAnUnknownTodoItem_ReturnsNotFound()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsync(
            ManifestUri(todoList.Id, Guid.CreateVersion7()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Item.NotFound", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_WithAnUnknownTodoList_ReturnsNotFound()
    {
        var response = await Client.PostAsync(
            ManifestUri(Guid.CreateVersion7(), Guid.CreateVersion7()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shape layer. An all-zero identifier is malformed, not absent — so 400 and the validator's
    /// code, which is also how a validator that was never registered would be caught.
    /// </summary>
    [Fact]
    public async Task Request_WithAnEmptyTodoItemIdentifier_ReturnsBadRequest()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsync(ManifestUri(todoList.Id, Guid.Empty), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Todo Item Id", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The requested event is dispatched. Nothing outside the request can see this, which is why it
    /// is asserted here and matched on the event id rather than on the message.
    /// </summary>
    [Fact]
    public async Task Request_WhenItSucceeds_DispatchesTheRequestedEvent()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        using (_fixture.Logs.Record())
        {
            var response = await Client.PostAsync(ManifestUri(todoList.Id, milk.Id), content: null);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        Assert.Contains(
            _fixture.Logs.Records,
            record => record.EventId.Id == ManifestationRequestedEventId);
    }

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri ManifestUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/manifest", UriKind.Relative);

    private static Uri ManifestationUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}", UriKind.Relative);

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
}
