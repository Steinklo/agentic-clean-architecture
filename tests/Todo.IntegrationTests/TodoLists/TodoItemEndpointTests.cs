using System.Net;
using System.Net.Http.Json;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.TodoLists;

/// <summary>
/// Adding and completing TodoItems, exercised over real HTTP against the real host and a real
/// database.
/// </summary>
/// <remarks>
/// <para>
/// This is the only seam these use cases are tested at, and it is the seam that can actually catch
/// what they get wrong. A TodoItem is mapped as a child of its aggregate, its description is a
/// value object squeezed through a value converter, and its collection is written through a private
/// backing field - three mappings whose failure modes are all invisible from inside a handler and
/// all obvious from out here, where the item either comes back on the TodoList or does not.
/// </para>
/// <para>
/// There is deliberately no test that mocks <c>ITodoListRepository</c> and asserts the handler
/// called it. See <see cref="TodoListEndpointTests"/>.
/// </para>
/// </remarks>
public sealed class TodoItemEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    [Fact]
    public async Task Post_WithAValidDescription_ReturnsTheCreatedTodoItem()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        // Untrimmed on purpose: trimming is the TodoItemDescription value object's rule, so a
        // trimmed description coming back proves the value object made the trip through the value
        // converter rather than a bare string column.
        var response = await Client.PostAsJsonAsync(ItemsUri(todoList.Id), new { Description = "  Buy milk  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.NotNull(item);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("Buy milk", item.Description);
        Assert.False(item.IsComplete);
        Assert.Null(item.CompletedAt);
        Assert.Equal($"/api/todo-lists/{todoList.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_AfterAddingItems_ReturnsTheTodoListWithEveryItem()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var bread = await AddTodoItemAsync(todoList.Id, "Buy bread");

        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.Equal(2, retrieved.Items.Count);
        Assert.Equal(milk, Assert.Single(retrieved.Items, item => item.Id == milk.Id));
        Assert.Equal(bread, Assert.Single(retrieved.Items, item => item.Id == bread.Id));
    }

    [Fact]
    public async Task Complete_WithAKnownTodoItem_MarksItCompleteOnTheTodoList()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var bread = await AddTodoItemAsync(todoList.Id, "Buy bread");

        var response = await Client.PostAsync(CompleteUri(todoList.Id, milk.Id), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var retrieved = await GetTodoListAsync(todoList.Id);
        var completed = Assert.Single(retrieved.Items, item => item.Id == milk.Id);

        Assert.True(completed.IsComplete);
        Assert.NotNull(completed.CompletedAt);

        // The write touched one item, not the aggregate's whole collection.
        Assert.False(Assert.Single(retrieved.Items, item => item.Id == bread.Id).IsComplete);
    }

    [Fact]
    public async Task Complete_WithAnUnknownTodoItem_ReturnsNotFound()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsync(
            CompleteUri(todoList.Id, Guid.CreateVersion7()),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Item.NotFound", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_ToAnUnknownTodoList_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            ItemsUri(Guid.CreateVersion7()),
            new { Description = "Buy milk" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Completing something already complete is a conflict, not a validation failure and not a
    /// success - and the single result translation is what turns that category into 409.
    /// </summary>
    [Fact]
    public async Task Complete_WithATodoItemAlreadyComplete_ReturnsConflict()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        var completed = await Client.PostAsync(CompleteUri(todoList.Id, milk.Id), content: null);

        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);

        var response = await Client.PostAsync(CompleteUri(todoList.Id, milk.Id), content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Item.AlreadyComplete", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shape layer: an absent description never reaches a handler.
    /// </summary>
    [Fact]
    public async Task Post_WithAnEmptyDescription_ReturnsBadRequestCarryingTheValidationFailure()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsJsonAsync(ItemsUri(todoList.Id), new { Description = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Description", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The domain layer, through the same endpoint. A two-character description is well-shaped -
    /// present, and inside the maximum the validator knows about - so it gets past FluentValidation
    /// and is refused by <c>TodoItemDescription.Create</c>, whose minimum length is the domain's
    /// rule and lives only there.
    /// </summary>
    [Fact]
    public async Task Post_WithADescriptionTheDomainRejects_ReturnsBadRequestCarryingTheDomainError()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsJsonAsync(ItemsUri(todoList.Id), new { Description = "ab" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoItem.Description.Length", body, StringComparison.Ordinal);
    }

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri CompleteUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/complete", UriKind.Relative);

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
        var response = await Client.PostAsJsonAsync(ItemsUri(todoListId), new { Description = description });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var added = await response.Content.ReadFromJsonAsync<TodoItemDto>();

        Assert.NotNull(added);

        return added;
    }

    private async Task<TodoListDto> GetTodoListAsync(Guid todoListId)
    {
        var response = await Client.GetAsync(new Uri($"/api/todo-lists/{todoListId}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(retrieved);

        return retrieved;
    }
}
