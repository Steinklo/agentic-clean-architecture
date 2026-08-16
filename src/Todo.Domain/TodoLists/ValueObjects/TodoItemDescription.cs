using System.Globalization;
using Todo.Domain.Common;

namespace Todo.Domain.TodoLists.ValueObjects;

/// <summary>
/// What a TodoItem asks someone to do. Trimmed, non-empty, and bounded in length.
/// </summary>
public sealed class TodoItemDescription : ValueObject
{
    /// <summary>Shortest permitted description, after trimming.</summary>
    public const int MinLength = 3;

    /// <summary>Longest permitted description, after trimming.</summary>
    public const int MaxLength = 500;

    private TodoItemDescription(string value) => Value = value;

    /// <summary>The trimmed description text.</summary>
    public string Value { get; private set; }

    /// <summary>
    /// Validates and creates a description, returning a validation failure rather than throwing.
    /// </summary>
    public static Result<TodoItemDescription> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TodoItemDescription>(DomainError.Validation(
                "TodoItem.Description.Empty",
                "A TodoItem description is required."));
        }

        var trimmed = value.Trim();

        return trimmed.Length is < MinLength or > MaxLength
            ? Result.Failure<TodoItemDescription>(DomainError.Validation(
                "TodoItem.Description.Length",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A TodoItem description must be between {MinLength} and {MaxLength} characters.")))
            : Result.Success(new TodoItemDescription(trimmed));
    }

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
