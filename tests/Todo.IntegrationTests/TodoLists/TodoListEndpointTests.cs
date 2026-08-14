using System.Net;
using System.Net.Http.Json;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.TodoLists;

/// <summary>
/// The TodoList endpoints, exercised over real HTTP against the real host and a real database.
/// </summary>
/// <remarks>
/// <para>
/// Every test here goes through the whole stack: routing, model binding, the validation behaviour,
/// the handler, the repository, the unit of work, EF Core, SQL Server, and back out through the
/// one result-to-response translation. That is deliberate and it is the only seam this solution
/// tests at.
/// </para>
/// <para>
/// There are no handler-tests-with-mocks. A test that mocks <c>ITodoListRepository</c> and asserts
/// the handler called <c>Add</c> proves the handler is the handler; it cannot fail for any reason
/// a user would notice, and it fails for every refactoring a user would not. The bugs that matter
/// here - a value object that does not survive the round trip, a validator that is registered but
/// never runs, a status code that is wrong - are all invisible from inside the handler and all
/// obvious from out here.
/// </para>
/// </remarks>
public sealed class TodoListEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    [Fact]
    public async Task PostThenGet_WithAValidTitle_RoundTripsEveryValueUnchanged()
    {
        // Untrimmed on purpose: trimming is the TodoListTitle value object's rule, so a trimmed
        // title coming back out proves the value object - not just a string column - made the trip.
        var created = await CreateTodoListAsync("  Weekend shopping  ");

        var response = await Client.GetAsync(TodoListUri(created.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrieved = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Weekend shopping", retrieved.Title);
        Assert.False(retrieved.IsArchived);
        Assert.Null(retrieved.ArchivedAt);
        Assert.Equal(created.CreatedAt, retrieved.CreatedAt);
        Assert.Empty(retrieved.Items);

        // Every value, whole, so a property added to the DTO later is covered here without anyone
        // remembering to add a line. The items are compared element-wise first and then held
        // constant, because a record compares a collection property by reference and two equal but
        // distinct lists would fail this for a reason that says nothing about the round trip.
        Assert.Equal(created.Items, retrieved.Items);
        Assert.Equal(created, retrieved with { Items = created.Items });
    }

    [Fact]
    public async Task Post_WithAValidTitle_ReturnsCreatedAtTheNewTodoList()
    {
        var response = await Client.PostAsJsonAsync(_todoLists, new CreateTodoListCommand("Weekend shopping"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal($"/api/todo-lists/{created.Id}", response.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// The shape layer: FluentValidation rejects a missing title before any handler runs, and the
    /// validation behaviour turns that into a Validation-categorised failure - which the single
    /// result translation renders as 400.
    /// </summary>
    [Fact]
    public async Task Post_WithAnEmptyTitle_ReturnsBadRequestCarryingTheValidationFailure()
    {
        var response = await Client.PostAsJsonAsync(_todoLists, new CreateTodoListCommand(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
        Assert.Contains("Title", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The domain layer, reached through the same endpoint and rendered by the same translation.
    /// A two-character title is well-shaped - present, and inside the maximum length the validator
    /// knows about - so it gets past FluentValidation and is refused by
    /// <c>TodoListTitle.Create</c>, whose minimum length is the domain's rule and lives only there.
    /// </summary>
    [Fact]
    public async Task Post_WithATitleTheDomainRejects_ReturnsBadRequestCarryingTheDomainError()
    {
        var response = await Client.PostAsJsonAsync(_todoLists, new CreateTodoListCommand("ab"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.Title.Length", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Queries go through the same validation behaviour as commands: an all-zero identifier is a
    /// malformed request, not a request for something that happens not to exist, so it is 400 and
    /// not 404.
    /// </summary>
    [Fact]
    public async Task Get_WithAnEmptyIdentifier_ReturnsBadRequest()
    {
        var response = await Client.GetAsync(TodoListUri(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_WithAnUnknownIdentifier_ReturnsNotFound()
    {
        var response = await Client.GetAsync(TodoListUri(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.NotFound", body, StringComparison.Ordinal);
    }

    private static Uri TodoListUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}", UriKind.Relative);

    private async Task<TodoListDto> CreateTodoListAsync(string title)
    {
        var response = await Client.PostAsJsonAsync(_todoLists, new CreateTodoListCommand(title));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TodoListDto>();

        Assert.NotNull(created);

        return created;
    }
}
