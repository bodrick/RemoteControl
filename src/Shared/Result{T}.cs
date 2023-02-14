using System.Runtime.Serialization;

namespace Immense.RemoteControl.Shared;

[DataContract]
public class Result<T>
{
    public Result(bool isSuccess, T? value, string? error = null, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
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

    [DataMember]
    public T? Value { get; init; }
}
