namespace FireBlazor;

/// <summary>
/// Represents the result of a Firebase operation that may succeed or fail.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly FirebaseError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on failed result. Error: {_error?.Message}");

    public FirebaseError? Error => _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(FirebaseError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(FirebaseError error) => error is null
        ? throw new ArgumentNullException(nameof(error))
        : new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<FirebaseError, TResult> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<FirebaseError, Task<TResult>> onFailure)
        => IsSuccess ? await onSuccess(_value!) : await onFailure(_error!);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess ? Result<TNew>.Success(mapper(_value!)) : Result<TNew>.Failure(_error!);

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
        => IsSuccess ? Result<TNew>.Success(await mapper(_value!)) : Result<TNew>.Failure(_error!);
}

/// <summary>
/// Represents an error from a Firebase operation.
/// </summary>
public sealed record FirebaseError(string Code, string Message)
{
    public override string ToString() => $"[{Code}] {Message}";
}
