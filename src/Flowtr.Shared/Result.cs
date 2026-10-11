namespace Flowtr.Shared;

/// <summary>
/// Outcome of a command without a return value.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// True if the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Error message if the operation failed, otherwise null.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a successful result carrying a value.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, null);

    /// <summary>
    /// Creates a failed result for an operation that would return a value.
    /// </summary>
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <summary>
/// Outcome of a query or of a command with a return value.
/// </summary>
public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// The value if the operation succeeded, otherwise default.
    /// </summary>
    public T? Value { get; }
}
