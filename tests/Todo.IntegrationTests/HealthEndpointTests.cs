using System.Net;

namespace Todo.IntegrationTests;

/// <summary>
/// Proves the whole seam: real HTTP, real host, real containerised SQL Server.
/// </summary>
public sealed class HealthEndpointTests(TodoApiFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetHealth_WhenDatabaseIsReachable_ReturnsOk()
    {
        var response = await Client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.Ordinal);
    }
}
