namespace Todo.IntegrationTests;

/// <summary>
/// Base class for every integration test: a fresh client against an empty database.
/// </summary>
/// <remarks>
/// The reset runs before each test rather than after, so a failed test leaves its data behind for
/// inspection while still not being able to influence the next one. Test ordering cannot matter.
/// </remarks>
[Collection(TodoApiCollection.Name)]
public abstract class IntegrationTestBase(TodoApiFixture fixture) : IAsyncLifetime
{
    private readonly TodoApiFixture _fixture = fixture;

    /// <summary>
    /// Speaks real HTTP to the in-process API. Assigned before each test runs.
    /// </summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        Client = _fixture.CreateClient();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }
}
