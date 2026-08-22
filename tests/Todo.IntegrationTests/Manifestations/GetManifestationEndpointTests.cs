using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Reading a Manifestation back, over real HTTP against a real database.
/// </summary>
/// <remarks>
/// This is the test that makes the slice honest. Everything below the API is fully implemented, and
/// the only way to see that from outside is to write a Manifestation, read it back through a second
/// request, and find it intact — mapping, migration and repository included. A slice that stubbed
/// its persistence would pass its own create test and fail this one.
/// </remarks>
public sealed class GetManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    [Fact]
    public async Task Get_WithAKnownManifestation_ReturnsItWithEveryFieldItWasCreatedWith()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var created = await RequestManifestationAsync(todoList.Id, milk.Id);

        var response = await Client.GetAsync(ManifestationUri(created.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(todoList.Id, retrieved.TodoListId);
        Assert.Equal(milk.Id, retrieved.TodoItemId);
        Assert.Equal("Requested", retrieved.State);
        Assert.NotEqual(default, retrieved.CreatedAt);
    }

    /// <summary>
    /// The state travels as its name and not as a number, so the contract survives a member being
    /// added to the enum. Asserted explicitly because the default serialiser behaviour is the other
    /// one.
    /// </summary>
    [Fact]
    public async Task Get_WithAKnownManifestation_WritesTheStateAsItsName()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var created = await RequestManifestationAsync(todoList.Id, milk.Id);

        var response = await Client.GetAsync(ManifestationUri(created.Id));

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"Requested\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_WithAnUnknownManifestation_ReturnsNotFound()
    {
        var response = await Client.GetAsync(ManifestationUri(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Manifestation.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// An all-zero identifier is malformed, not absent, so it is a 400 and not the 404 above. The
    /// pair is what proves the query's validator is registered and running.
    /// </summary>
    [Fact]
    public async Task Get_WithAnEmptyIdentifier_ReturnsBadRequest()
    {
        var response = await Client.GetAsync(ManifestationUri(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Manifestation Id", body, StringComparison.Ordinal);
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
}
