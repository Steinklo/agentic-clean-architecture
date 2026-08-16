using System.Net;
using System.Net.Http.Json;
using Todo.Application.Manifestations.Dtos;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.Manifestations;

/// <summary>
/// Reading a Manifestation back, exercised over real HTTP against the real host and a real database.
/// </summary>
public sealed class GetManifestationEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    [Fact]
    public async Task Get_WithAKnownIdentifier_ReturnsTheManifestation()
    {
        var created = await RequestManifestationAsync();

        var response = await Client.GetAsync(ManifestationUri(created.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<ManifestationDto>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.TodoListId, retrieved.TodoListId);
        Assert.Equal(created.TodoItemId, retrieved.TodoItemId);
        Assert.Equal(created.CreatedAt, retrieved.CreatedAt);

        // The state is projected as its name, so reordering the enum cannot quietly re-label it.
        Assert.Equal("Requested", retrieved.State);
    }

    [Fact]
    public async Task Get_WithAnUnknownIdentifier_ReturnsNotFound()
    {
        var response = await Client.GetAsync(ManifestationUri(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Manifestation.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The query goes through the same validation behaviour as a command, so an all-zero identifier
    /// is 400 rather than 404.
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

    private static Uri ManifestationUri(Guid manifestationId) =>
        new($"/api/manifestations/{manifestationId}", UriKind.Relative);

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri ManifestUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/manifest", UriKind.Relative);

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
