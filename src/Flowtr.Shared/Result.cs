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

    public bool IsSuccess { get; }

    /// <summary>
    /// Null if the operation succeeded.
    /// </summary>
    public string? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

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
    /// Default if the operation failed.
    /// </summary>
    public T? Value { get; }
}
