namespace Todo.IntegrationTests;

/// <summary>
/// Binds every integration test to the one shared <see cref="TodoApiFixture"/>.
/// </summary>
/// <remarks>
/// A collection fixture, not a class fixture: xUnit builds it once for the whole collection, so the
/// SQL Server container starts once no matter how many test classes join. xUnit also runs the
/// classes in a collection one at a time, which is what makes a between-test database reset safe.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class TodoApiCollection : ICollectionFixture<TodoApiFixture>
{
    /// <summary>
    /// The collection name. Referenced by <see cref="IntegrationTestBase"/>.
    /// </summary>
    public const string Name = "Todo API";
}
