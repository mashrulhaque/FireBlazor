namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IOnDisconnect.
/// </summary>
internal sealed class WasmOnDisconnect : IOnDisconnect
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;

    public WasmOnDisconnect(FirebaseJsInterop jsInterop, string path)
    {
        _jsInterop = jsInterop;
        _path = path;
    }

    public async Task<Result<Unit>> SetAsync<T>(T value)
    {
        try
        {
            var result = await _jsInterop.DatabaseOnDisconnectSetAsync(_path, value!);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult("database/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> RemoveAsync()
    {
        try
        {
            var result = await _jsInterop.DatabaseOnDisconnectRemoveAsync(_path);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult("database/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> UpdateAsync(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        try
        {
            var result = await _jsInterop.DatabaseOnDisconnectUpdateAsync(_path, value);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult("database/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> CancelAsync()
    {
        try
        {
            var result = await _jsInterop.DatabaseOnDisconnectCancelAsync(_path);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult("database/unknown", ex.Message);
        }
    }

    private static Result<Unit> CreateFailureFromJsError(JsError? error)
    {
        var code = error?.Code ?? "database/unknown";
        var message = error?.Message ?? "Unknown error";
        var dbCode = DatabaseErrorCodeExtensions.FromFirebaseCode(code);
        return Result<Unit>.Failure(new FirebaseError(dbCode.ToFirebaseCode(), message));
    }

    private static Result<Unit> CreateFailureResult(string code, string message)
    {
        var dbCode = DatabaseErrorCodeExtensions.FromFirebaseCode(code);
        return Result<Unit>.Failure(new FirebaseError(dbCode.ToFirebaseCode(), message));
    }
}
