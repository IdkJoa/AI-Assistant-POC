namespace AiAssistant.Domain.Common.OperationResult;

public sealed class Result<T>
{
    private Result(T value)
    {
        Value = value;
        Error = Error.None;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
        IsSuccess = false;
    }

    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed class Result
{
    private Result() => IsSuccess = true;
    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public Error Error { get; } = Error.None;
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);
}