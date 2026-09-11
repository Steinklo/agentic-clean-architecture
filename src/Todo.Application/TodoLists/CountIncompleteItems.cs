using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Todo.Domain.Common;
using Todo.Domain.TodoLists.Entities;

namespace Todo.Application.TodoLists;

/// <summary>
/// Counts how many items on a TodoList are still incomplete.
/// </summary>
/// <param name="TodoListId">Identity of the TodoList to count items on.</param>
public sealed record CountIncompleteItemsQuery(Guid TodoListId) : IRequest<Result<int>>;

/// <summary>
/// Shape rules for <see cref="CountIncompleteItemsQuery"/>.
/// </summary>
public sealed class CountIncompleteItemsQueryValidator : AbstractValidator<CountIncompleteItemsQuery>
{
    /// <summary>Declares the rules.</summary>
    public CountIncompleteItemsQueryValidator() =>
        RuleFor(query => query.TodoListId).NotEmpty();
}

/// <summary>
/// Answers <see cref="CountIncompleteItemsQuery"/>.
/// </summary>
internal sealed class CountIncompleteItemsHandler(DbContext dbContext)
    : IRequestHandler<CountIncompleteItemsQuery, Result<int>>
{
    private readonly DbContext _dbContext = dbContext;

    public async ValueTask<Result<int>> Handle(
        CountIncompleteItemsQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _dbContext.Set<TodoItem>()
            .Where(item => item.TodoListId == request.TodoListId && !item.IsComplete)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(count);
    }
}
