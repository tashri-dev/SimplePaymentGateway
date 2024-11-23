namespace SimplePaymentGateway.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public ResultType Type { get; private set; }

    protected Result(bool isSuccess, T? value, string? error, ResultType type)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Type = type;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, ResultType.Success);

    public static Result<T> Failure(string error, ResultType type = ResultType.Error) =>
        new(false, default, error, type);
}
public enum ResultType
{
    Success,
    ValidationError,
    BadRequest,
    NotFound,
    Error
}
