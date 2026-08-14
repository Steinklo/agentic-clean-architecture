namespace Todo.Api.Endpoints;

/// <summary>
/// One minimal-API endpoint, mapped into its feature's route group.
/// </summary>
/// <remarks>
/// Implementations are found by scanning this assembly (see <see cref="EndpointExtensions"/>), so
/// adding an endpoint is adding a file. There is no central list of routes to keep in step - which
/// is the failure mode this replaces: an endpoint written, never registered, and never noticed.
/// </remarks>
internal interface IEndpoint
{
    /// <summary>
    /// The feature's route prefix, for example <c>/api/todo-lists</c>. Endpoints that share a
    /// prefix are mapped into one route group.
    /// </summary>
    string GroupPrefix { get; }

    /// <summary>The feature's OpenAPI tag, for example <c>TodoLists</c>.</summary>
    string GroupTag { get; }

    /// <summary>
    /// Maps this endpoint onto its feature's group.
    /// </summary>
    /// <param name="group">The group for <see cref="GroupPrefix"/>, created once per feature.</param>
    void MapEndpoint(RouteGroupBuilder group);
}
