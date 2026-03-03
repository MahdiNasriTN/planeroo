namespace Planeroo.Domain.Common;

/// <summary>
/// Result pattern for clean error handling without exceptions.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        StatusCode = 200;
    }

    private Result(string error, int statusCode = 400)
    {
        IsSuccess = false;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, int statusCode = 400) => new(error, statusCode);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(bool success, string? error = null, int statusCode = 200)
    {
        IsSuccess = success;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
}
