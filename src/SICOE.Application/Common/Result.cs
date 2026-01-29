namespace SICOE.Application.Common;

/// <summary>
/// Result Pattern para manejo de errores (OCP: Extensible sin modificar)
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; private set; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Resultado exitoso no puede tener error");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Resultado fallido debe tener error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new Result(true, string.Empty);
    public static Result Failure(string error) => new Result(false, error);
}

/// <summary>
/// Result Pattern genérico con valor
/// </summary>
public class Result<T> : Result
{
    public T Value { get; private set; }

    private Result(bool isSuccess, T value, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);
    public static new Result<T> Failure(string error) => new Result<T>(false, default(T)!, error);
}

