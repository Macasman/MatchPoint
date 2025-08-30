namespace MatchPoint.Shared.Results;
public class Result<T>
{
    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public T? Data { get; private set; }

    private Result(bool success, T? data, string? error)
    {
        Success = success;
        Data = data;
        Error = error;
    }

    public static Result<T> Ok(T data) => new Result<T>(true, data, null);
    public static Result<T> Fail(string error) => new Result<T>(false, default, error);
}
