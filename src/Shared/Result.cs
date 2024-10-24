using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared;

[DataContract]
public class Result
{
    public Result(bool isSuccess, string? error = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;

        if (!IsSuccess && string.IsNullOrWhiteSpace(Error) && Exception is null)
        {
            throw new ArgumentException("An error message or exception must be supplied for an unsuccessful result.");
        }

        if (string.IsNullOrWhiteSpace(Error) && Exception is not null)
        {
            Error = Exception.Message;
        }

        if (Exception is null && !string.IsNullOrWhiteSpace(Error))
        {
            Exception = new Exception(Error);
        }
    }

    [DataMember]
    public string? Error { get; init; }

    [DataMember]
    public Exception? Exception { get; init; }

    [DataMember]
    public bool IsSuccess { get; init; }

    public static Result<T> Empty<T>() => new(true, default);

    public static Result Fail(string error) => new(false, error);

    public static Result Fail(Exception ex) => new(false, null, ex);

    public static Result<T> Fail<T>(string error) => new(false, default, error);

    public static Result<T> Fail<T>(Exception ex) => new(false, default, exception: ex);

    public static Result Ok() => new(true);

    public static Result<T> Ok<T>(T value) => new(true, value);
}
