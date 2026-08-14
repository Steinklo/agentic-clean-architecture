using Todo.Domain.Common;
using Todo.Domain.TodoLists;
using Todo.Domain.TodoLists.Events;
using Todo.Domain.TodoLists.ValueObjects;

namespace Todo.UnitTests.TodoLists;

/// <summary>
/// The domain is tested at one seam only: the aggregate root. Every case constructs a real
/// TodoList and asserts on the returned Result. No mocks, no substitutes, no I/O — if a test
/// here needs a test double, the invariant has leaked out of the aggregate.
/// </summary>
public class TodoListTests
{
    private const string ValidTitle = "Groceries";
    private const string ValidDescription = "Buy milk";

    [Fact]
    public void Create_WithValidTitle_ReturnsSuccess()
    {
        var result = TodoList.Create(ValidTitle);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidTitle, result.Value.Title.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Empty(result.Value.Items);
        Assert.False(result.Value.IsArchived);
        Assert.Null(result.Value.ArchivedAt);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsTitle()
    {
        var result = TodoList.Create($"  {ValidTitle}  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidTitle, result.Value.Title.Value);
    }

    [Theory]
    [InlineData(null, "TodoList.Title.Empty")]
    [InlineData("", "TodoList.Title.Empty")]
    [InlineData("   ", "TodoList.Title.Empty")]
    [InlineData("\t\r\n", "TodoList.Title.Empty")]
    [InlineData("ab", "TodoList.Title.Length")]
    [InlineData("  a  ", "TodoList.Title.Length")]
    public void Create_WithInvalidTitle_ReturnsValidationError(string? title, string expectedCode)
    {
        var result = TodoList.Create(title!);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public void Create_WithTitleLongerThanMaximum_ReturnsValidationError()
    {
        var result = TodoList.Create(new string('a', TodoListTitle.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Validation, result.Error.Type);
        Assert.Equal("TodoList.Title.Length", result.Error.Code);
    }

    [Fact]
    public void Create_WithTitleAtMaximumLength_ReturnsSuccess()
    {
        var result = TodoList.Create(new string('a', TodoListTitle.MaxLength));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithValidTitle_RaisesTodoListCreatedEvent()
    {
        var todoList = CreateTodoList();

        var created = Assert.Single(todoList.DomainEvents.OfType<TodoListCreatedEvent>());
        Assert.Equal(todoList.Id, created.TodoListId);
        Assert.Equal(todoList.Id, created.AggregateId);
        Assert.Equal(ValidTitle, created.Title);
    }

    [Fact]
    public void ClearDomainEvents_AfterCreate_RemovesRaisedEvents()
    {
        var todoList = CreateTodoList();

        todoList.ClearDomainEvents();

        Assert.Empty(todoList.DomainEvents);
    }

    [Fact]
    public void AddItem_WithValidDescription_ReturnsSuccessAndAddsItem()
    {
        var todoList = CreateTodoList();

        var result = todoList.AddItem(ValidDescription);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidDescription, result.Value.Description.Value);
        Assert.Equal(todoList.Id, result.Value.TodoListId);
        Assert.False(result.Value.IsComplete);
        Assert.Null(result.Value.CompletedAt);
        Assert.Same(result.Value, Assert.Single(todoList.Items));
    }

    [Fact]
    public void AddItem_CalledTwice_AddsBothItems()
    {
        var todoList = CreateTodoList();

        var first = todoList.AddItem("Buy milk");
        var second = todoList.AddItem("Buy bread");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, todoList.Items.Count);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
    }

    [Theory]
    [InlineData(null, "TodoItem.Description.Empty")]
    [InlineData("", "TodoItem.Description.Empty")]
    [InlineData("   ", "TodoItem.Description.Empty")]
    [InlineData("\t\r\n", "TodoItem.Description.Empty")]
    [InlineData("ab", "TodoItem.Description.Length")]
    [InlineData("  a  ", "TodoItem.Description.Length")]
    public void AddItem_WithInvalidDescription_ReturnsValidationError(string? description, string expectedCode)
    {
        var todoList = CreateTodoList();

        var result = todoList.AddItem(description!);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Empty(todoList.Items);
    }

    [Fact]
    public void AddItem_WithDescriptionLongerThanMaximum_ReturnsValidationError()
    {
        var todoList = CreateTodoList();

        var result = todoList.AddItem(new string('a', TodoItemDescription.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Validation, result.Error.Type);
        Assert.Equal("TodoItem.Description.Length", result.Error.Code);
        Assert.Empty(todoList.Items);
    }

    [Fact]
    public void AddItem_OnArchivedTodoList_ReturnsConflict()
    {
        var todoList = CreateArchivedTodoList();

        var result = todoList.AddItem(ValidDescription);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("TodoList.Archived", result.Error.Code);
        Assert.Empty(todoList.Items);
    }

    [Fact]
    public void CompleteItem_WithIncompleteItem_ReturnsSuccessAndMarksItemComplete()
    {
        var todoList = CreateTodoList();
        var item = todoList.AddItem(ValidDescription).Value;

        var result = todoList.CompleteItem(item.Id);

        Assert.True(result.IsSuccess);
        Assert.True(item.IsComplete);
        Assert.NotNull(item.CompletedAt);
    }

    [Fact]
    public void CompleteItem_WithIncompleteItem_RaisesTodoItemCompletedEvent()
    {
        var todoList = CreateTodoList();
        var item = todoList.AddItem(ValidDescription).Value;

        todoList.CompleteItem(item.Id);

        var completed = Assert.Single(todoList.DomainEvents.OfType<TodoItemCompletedEvent>());
        Assert.Equal(todoList.Id, completed.TodoListId);
        Assert.Equal(item.Id, completed.TodoItemId);
    }

    [Fact]
    public void CompleteItem_WithUnknownTodoItemId_ReturnsNotFound()
    {
        var todoList = CreateTodoList();
        todoList.AddItem(ValidDescription);

        var result = todoList.CompleteItem(Guid.CreateVersion7());

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.NotFound, result.Error.Type);
        Assert.Equal("TodoList.Item.NotFound", result.Error.Code);
    }

    [Fact]
    public void CompleteItem_WithItemBelongingToAnotherTodoList_ReturnsNotFound()
    {
        var todoList = CreateTodoList();
        var otherList = CreateTodoList("Other list");
        var otherItem = otherList.AddItem(ValidDescription).Value;

        var result = todoList.CompleteItem(otherItem.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.NotFound, result.Error.Type);
        Assert.False(otherItem.IsComplete);
    }

    [Fact]
    public void CompleteItem_WithAlreadyCompleteItem_ReturnsConflict()
    {
        var todoList = CreateTodoList();
        var item = todoList.AddItem(ValidDescription).Value;
        todoList.CompleteItem(item.Id);

        var result = todoList.CompleteItem(item.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("TodoList.Item.AlreadyComplete", result.Error.Code);
        Assert.Single(todoList.DomainEvents.OfType<TodoItemCompletedEvent>());
    }

    [Fact]
    public void CompleteItem_OnArchivedTodoList_ReturnsConflict()
    {
        var todoList = CreateTodoList();
        var item = todoList.AddItem(ValidDescription).Value;
        todoList.CompleteItem(item.Id);
        todoList.Archive();

        var result = todoList.CompleteItem(item.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("TodoList.Archived", result.Error.Code);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void Archive_WithIncompleteItem_ReturnsConflict(int itemCount, int completedCount)
    {
        var todoList = CreateTodoList(itemCount, completedCount);

        var result = todoList.Archive();

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("TodoList.Archive.IncompleteItems", result.Error.Code);
        Assert.False(todoList.IsArchived);
        Assert.Null(todoList.ArchivedAt);
        Assert.Empty(todoList.DomainEvents.OfType<TodoListArchivedEvent>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Archive_WithEveryItemComplete_ReturnsSuccess(int itemCount)
    {
        var todoList = CreateTodoList(itemCount, itemCount);

        var result = todoList.Archive();

        Assert.True(result.IsSuccess);
        Assert.True(todoList.IsArchived);
        Assert.NotNull(todoList.ArchivedAt);
    }

    [Fact]
    public void Archive_WithEveryItemComplete_RaisesTodoListArchivedEvent()
    {
        var todoList = CreateTodoList(itemCount: 2, completedCount: 2);

        todoList.Archive();

        var archived = Assert.Single(todoList.DomainEvents.OfType<TodoListArchivedEvent>());
        Assert.Equal(todoList.Id, archived.TodoListId);
        Assert.Equal(todoList.Id, archived.AggregateId);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ReturnsConflict()
    {
        var todoList = CreateArchivedTodoList();

        var result = todoList.Archive();

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
        Assert.Equal("TodoList.AlreadyArchived", result.Error.Code);
        Assert.Single(todoList.DomainEvents.OfType<TodoListArchivedEvent>());
    }

    private static TodoList CreateTodoList(string title = ValidTitle)
    {
        var result = TodoList.Create(title);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    private static TodoList CreateTodoList(int itemCount, int completedCount)
    {
        var todoList = CreateTodoList();

        for (var index = 0; index < itemCount; index++)
        {
            var item = todoList.AddItem($"Item {index}").Value;

            if (index < completedCount)
            {
                Assert.True(todoList.CompleteItem(item.Id).IsSuccess);
            }
        }

        return todoList;
    }

    private static TodoList CreateArchivedTodoList()
    {
        var todoList = CreateTodoList();

        Assert.True(todoList.Archive().IsSuccess);

        return todoList;
    }
}
