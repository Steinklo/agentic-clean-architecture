using System.Globalization;
using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.ValueObjects;

/// <summary>
/// The name a TodoList is known by. Trimmed, non-empty, and bounded in length.
/// </summary>
public sealed class TodoListTitle : ValueObject
{
    /// <summary>Shortest permitted title, after trimming.</summary>
    public const int MinLength = 3;

    /// <summary>Longest permitted title, after trimming.</summary>
    public const int MaxLength = 100;

    private TodoListTitle(string value) => Value = value;

    /// <summary>The trimmed title text.</summary>
    public string Value { get; private set; }

    /// <summary>
    /// Validates and creates a title, returning a validation failure rather than throwing.
    /// </summary>
    public static Result<TodoListTitle> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TodoListTitle>(DomainError.Validation(
                "TodoList.Title.Empty",
                "A TodoList title is required."));
        }

        var trimmed = value.Trim();

        return trimmed.Length is < MinLength or > MaxLength
            ? Result.Failure<TodoListTitle>(DomainError.Validation(
                "TodoList.Title.Length",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A TodoList title must be between {MinLength} and {MaxLength} characters.")))
            : Result.Success(new TodoListTitle(trimmed));
    }

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
