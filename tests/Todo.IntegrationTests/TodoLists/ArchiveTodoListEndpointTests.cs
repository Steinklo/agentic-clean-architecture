using System.Net;
using System.Net.Http.Json;
using Todo.Application.TodoLists.Commands.CreateTodoList;
using Todo.Application.TodoLists.Dtos;

namespace Todo.IntegrationTests.TodoLists;

/// <summary>
/// Archiving a TodoList, exercised over real HTTP against the real host and a real database.
/// </summary>
/// <remarks>
/// <para>
/// This is the invariant the aggregate boundary exists to protect, and this is the only seam that
/// can prove it holds. The rule - a TodoList cannot be archived while any of its TodoItems is
/// incomplete - is decidable only by something holding the whole aggregate at once, so it is
/// simultaneously a test of the domain rule, of the repository loading the Items with the TodoList,
/// and of the conflict reaching the caller as something it can act on.
/// </para>
/// <para>
/// There is deliberately no test that mocks <c>ITodoListRepository</c> and asserts the handler asked
/// the aggregate to archive itself. See <see cref="TodoListEndpointTests"/>.
/// </para>
/// </remarks>
public sealed class ArchiveTodoListEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// EF Core's category for executed database commands. Matched on rather than parsed for, so the
    /// ordering assertion below reads the log the same way a person would.
    /// </summary>
    private const string DatabaseCommandCategory = "Microsoft.EntityFrameworkCore.Database.Command";

    /// <summary>
    /// The event id <c>TodoListEventLog.TodoListArchived</c> declares. Asserting on the id rather
    /// than the message means rewording the message does not silently break this test.
    /// </summary>
    private const int TodoListArchivedEventId = 2002;

    private static readonly Uri _todoLists = new("/api/todo-lists", UriKind.Relative);

    private readonly TodoApiFixture _fixture = fixture;

    [Fact]
    public async Task Archive_WithEveryTodoItemComplete_ArchivesTheTodoList()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        var bread = await AddTodoItemAsync(todoList.Id, "Buy bread");

        await CompleteTodoItemAsync(todoList.Id, milk.Id);
        await CompleteTodoItemAsync(todoList.Id, bread.Id);

        var response = await Client.PostAsync(ArchiveUri(todoList.Id), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The archived state is a persisted fact, not something the response asserted about itself.
        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.True(retrieved.IsArchived);
        Assert.NotNull(retrieved.ArchivedAt);
        Assert.Equal(2, retrieved.Items.Count);
        Assert.All(retrieved.Items, item => Assert.True(item.IsComplete));
    }

    /// <summary>
    /// The invariant, refused. The domain categorises this as a conflict and the one result
    /// translation turns that into 409 - the endpoint names no status code of its own.
    /// </summary>
    [Fact]
    public async Task Archive_WithAnIncompleteTodoItem_ReturnsConflictCarryingTheDomainError()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");
        await AddTodoItemAsync(todoList.Id, "Buy bread");

        await CompleteTodoItemAsync(todoList.Id, milk.Id);

        var response = await Client.PostAsync(ArchiveUri(todoList.Id), content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // The stable code, not the prose: a caller branches on this, and a test that matched the
        // message would fail on a reword and pass on a renamed error.
        Assert.Contains("TodoList.Archive.IncompleteItems", body, StringComparison.Ordinal);

        // A refused transition changed nothing.
        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.False(retrieved.IsArchived);
        Assert.Null(retrieved.ArchivedAt);
    }

    /// <summary>
    /// A TodoList with no TodoItems archives, because "no incomplete TodoItems" is vacuously true of
    /// an empty one. That is the domain's deliberate answer, asserted here so it cannot later be
    /// "fixed" into a refusal without someone consciously deleting this test.
    /// </summary>
    [Fact]
    public async Task Archive_WithNoTodoItems_ArchivesTheTodoList()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        var response = await Client.PostAsync(ArchiveUri(todoList.Id), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.True(retrieved.IsArchived);
        Assert.NotNull(retrieved.ArchivedAt);
        Assert.Empty(retrieved.Items);
    }

    /// <summary>
    /// An archived TodoList is closed to change, and both ways of changing one are refused by the
    /// aggregate rather than by the endpoints - which is why the same error code comes back from
    /// two different routes.
    /// </summary>
    [Fact]
    public async Task Post_ToAnArchivedTodoList_IsRefused()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        await CompleteTodoItemAsync(todoList.Id, milk.Id);
        await ArchiveTodoListAsync(todoList.Id);

        var added = await Client.PostAsJsonAsync(ItemsUri(todoList.Id), new { Description = "Buy bread" });

        Assert.Equal(HttpStatusCode.Conflict, added.StatusCode);
        Assert.Contains(
            "TodoList.Archived",
            await added.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        // Completion is refused for being archived, not for the item already being complete: the
        // aggregate asks whether it accepts changes at all before it looks for the TodoItem.
        var completed = await Client.PostAsync(CompleteUri(todoList.Id, milk.Id), content: null);

        Assert.Equal(HttpStatusCode.Conflict, completed.StatusCode);
        Assert.Contains(
            "TodoList.Archived",
            await completed.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        // And nothing got through.
        var retrieved = await GetTodoListAsync(todoList.Id);

        Assert.True(retrieved.IsArchived);
        Assert.Single(retrieved.Items);
    }

    /// <summary>
    /// Archiving twice is a conflict, not a second success.
    /// </summary>
    /// <remarks>
    /// This is chosen, not inherited. Making the second call a 204 would mean the endpoint - or the
    /// handler - recognising <c>TodoList.AlreadyArchived</c> and rewriting it as success, which is
    /// per-endpoint status mapping under another name and the exact defect this template exists to
    /// avoid. It would also be a lie the caller cannot detect: the second caller did not archive
    /// anything, and the <c>ArchivedAt</c> they would then read back is somebody else's timestamp.
    /// A 409 naming <c>TodoList.AlreadyArchived</c> says precisely what happened, and a caller who
    /// wants idempotence can treat that one code as success - which they can only do because the
    /// body carries it.
    /// </remarks>
    [Fact]
    public async Task Archive_WithATodoListAlreadyArchived_ReturnsConflict()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");

        await ArchiveTodoListAsync(todoList.Id);

        var response = await Client.PostAsync(ArchiveUri(todoList.Id), content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.AlreadyArchived", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Archive_WithAnUnknownTodoList_ReturnsNotFound()
    {
        var response = await Client.PostAsync(ArchiveUri(Guid.CreateVersion7()), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("TodoList.NotFound", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shape layer, which for this command is the whole of it: an all-zero identifier is a
    /// malformed request, not a request for something that happens not to exist. Present here so
    /// the validator cannot be registered-but-never-run without a test noticing.
    /// </summary>
    [Fact]
    public async Task Archive_WithAnEmptyIdentifier_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(ArchiveUri(Guid.Empty), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation.Failed", body, StringComparison.Ordinal);

        // FluentValidation splits the property name into words, so the detail names the field the
        // caller has to fix rather than just saying something was wrong.
        Assert.Contains("Todo List Id", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The archival domain event is dispatched inside the same transaction as the save that commits
    /// it, and this proves it rather than assuming it.
    /// </summary>
    /// <remarks>
    /// "Same transaction" is observable from out here as an ordering plus a count. The unit of work
    /// publishes domain events and <em>then</em> calls <c>SaveChangesAsync</c> once, so if that
    /// holds, the archived-event handler must log before the database is told anything, and the
    /// whole request must contain exactly one write. Dispatching after the save would put the
    /// handler's log after the UPDATE; giving the dispatch its own save would put a second write in
    /// the request. Either regression fails one of the two assertions below, which is the point -
    /// the ordering is a guarantee, not an implementation detail that happens to hold today.
    /// </remarks>
    [Fact]
    public async Task Archive_WhenItSucceeds_DispatchesTheArchivedEventBeforeTheWriteThatCommitsIt()
    {
        var todoList = await CreateTodoListAsync("Weekend shopping");
        var milk = await AddTodoItemAsync(todoList.Id, "Buy milk");

        await CompleteTodoItemAsync(todoList.Id, milk.Id);

        // Recording opens here, so what is captured is exactly the archive request and nothing that
        // set it up.
        using (_fixture.Logs.Record())
        {
            var response = await Client.PostAsync(ArchiveUri(todoList.Id), content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        var records = _fixture.Logs.Records.ToList();

        var dispatched = records.FindIndex(record => record.EventId.Id == TodoListArchivedEventId);

        Assert.True(
            dispatched >= 0,
            "TodoListArchivedEvent was never dispatched: no handler for it logged during the request.");

        var writes = records.FindAll(IsDatabaseWrite);

        Assert.True(
            writes.Count == 1,
            $"Archiving should be one write in one transaction, but the request made {writes.Count}.");

        var written = records.FindIndex(IsDatabaseWrite);

        Assert.True(
            dispatched < written,
            "The archived event was dispatched after the write, so its handler could not have taken "
                + "part in the same transaction.");
    }

    /// <summary>
    /// Whether a captured record is EF Core reporting a statement that changed the database.
    /// </summary>
    private static bool IsDatabaseWrite(LogRecord record) =>
        record.Category.Equals(DatabaseCommandCategory, StringComparison.Ordinal)
        && (record.Message.Contains("UPDATE ", StringComparison.Ordinal)
            || record.Message.Contains("INSERT ", StringComparison.Ordinal)
            || record.Message.Contains("DELETE ", StringComparison.Ordinal));

    private static Uri TodoListUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}", UriKind.Relative);

    private static Uri ItemsUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/items", UriKind.Relative);

    private static Uri CompleteUri(Guid todoListId, Guid todoItemId) =>
        new($"/api/todo-lists/{todoListId}/items/{todoItemId}/complete", UriKind.Relative);

    private static Uri ArchiveUri(Guid todoListId) =>
        new($"/api/todo-lists/{todoListId}/archive", UriKind.Relative);

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

    private async Task CompleteTodoItemAsync(Guid todoListId, Guid todoItemId)
    {
        var response = await Client.PostAsync(CompleteUri(todoListId, todoItemId), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task ArchiveTodoListAsync(Guid todoListId)
    {
        var response = await Client.PostAsync(ArchiveUri(todoListId), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
