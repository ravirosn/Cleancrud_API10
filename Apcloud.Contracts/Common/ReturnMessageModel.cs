namespace Apcloud.Contracts.Common;

public class ReturnMessageModel
{
    public bool IsSuccess { get; init; }

    public string ReturnMessage { get; init; } = string.Empty;

    public int HttpStatusCode { get; init; }

    public static ReturnMessageModel Success(string message, int statusCode = 200) => new()
    {
        IsSuccess = true,
        ReturnMessage = message,
        HttpStatusCode = statusCode
    };

    public static ReturnMessageModel Failure(string message, int statusCode) => new()
    {
        IsSuccess = false,
        ReturnMessage = message,
        HttpStatusCode = statusCode
    };
}

public sealed class ReturnMessageModel<T> : ReturnMessageModel
{
    public T? Data { get; init; }

    public static ReturnMessageModel<T> Success(T data, string message, int statusCode = 200) => new()
    {
        IsSuccess = true,
        ReturnMessage = message,
        HttpStatusCode = statusCode,
        Data = data
    };

    public new static ReturnMessageModel<T> Failure(string message, int statusCode) => new()
    {
        IsSuccess = false,
        ReturnMessage = message,
        HttpStatusCode = statusCode
    };
}
