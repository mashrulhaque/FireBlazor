using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IDatabaseReference using JavaScript interop.
/// This is an immutable, chainable reference that builds up query state.
/// </summary>
internal sealed class WasmDatabaseReference : IDatabaseReference
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly DatabaseQueryParams _queryParams;

    public WasmDatabaseReference(FirebaseJsInterop jsInterop, string path)
        : this(jsInterop, path, new DatabaseQueryParams())
    {
    }

    private WasmDatabaseReference(FirebaseJsInterop jsInterop, string path, DatabaseQueryParams queryParams)
    {
        ArgumentNullException.ThrowIfNull(jsInterop);
        ArgumentNullException.ThrowIfNull(path);
        _jsInterop = jsInterop;
        // Normalize path: empty string or "/" represents root
        _path = path.TrimStart('/');
        _queryParams = queryParams;
    }

    public string Key => _path.Contains('/') ? _path.Split('/').Last() : _path;

    public string Path => _path;

    public IDatabaseReference Child(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var combinedPath = $"{_path.TrimEnd('/')}/{path.TrimStart('/')}";
        return new WasmDatabaseReference(_jsInterop, combinedPath, _queryParams);
    }

    public IDatabaseReference? Parent
    {
        get
        {
            var lastSlash = _path.LastIndexOf('/');
            if (lastSlash <= 0)
                return null;

            var parentPath = _path[..lastSlash];
            return new WasmDatabaseReference(_jsInterop, parentPath);
        }
    }

    // Query Methods - Return new immutable references with updated query params

    public IDatabaseReference OrderByChild(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (_queryParams.OrderBy != null)
            throw new InvalidOperationException($"Cannot use OrderByChild when OrderBy{GetOrderByMethodName(_queryParams.OrderBy)} is already set. Firebase only supports one ordering constraint.");
        var newParams = _queryParams with { OrderBy = "child", OrderByPath = path };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference OrderByKey()
    {
        if (_queryParams.OrderBy != null)
            throw new InvalidOperationException($"Cannot use OrderByKey when OrderBy{GetOrderByMethodName(_queryParams.OrderBy)} is already set. Firebase only supports one ordering constraint.");
        var newParams = _queryParams with { OrderBy = "key" };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference OrderByValue()
    {
        if (_queryParams.OrderBy != null)
            throw new InvalidOperationException($"Cannot use OrderByValue when OrderBy{GetOrderByMethodName(_queryParams.OrderBy)} is already set. Firebase only supports one ordering constraint.");
        var newParams = _queryParams with { OrderBy = "value" };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference LimitToFirst(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
        if (_queryParams.LimitToLast.HasValue)
            throw new InvalidOperationException("Cannot use LimitToFirst when LimitToLast is already set. Firebase only supports one limit constraint.");
        var newParams = _queryParams with { LimitToFirst = count };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference LimitToLast(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
        if (_queryParams.LimitToFirst.HasValue)
            throw new InvalidOperationException("Cannot use LimitToLast when LimitToFirst is already set. Firebase only supports one limit constraint.");
        var newParams = _queryParams with { LimitToLast = count };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference StartAt(object value, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        var newParams = _queryParams with { StartAtValue = value, StartAtKey = key };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference EndAt(object value, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        var newParams = _queryParams with { EndAtValue = value, EndAtKey = key };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    public IDatabaseReference EqualTo(object value, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        var newParams = _queryParams with { EqualToValue = value, EqualToKey = key };
        return new WasmDatabaseReference(_jsInterop, _path, newParams);
    }

    // CRUD Operations

    public async Task<Result<DataSnapshot<T>>> GetAsync<T>()
    {
        try
        {
            JsResult<JsDataSnapshot> result;

            if (_queryParams.HasQuery)
            {
                result = await _jsInterop.DatabaseQueryAsync(_path, _queryParams.ToJsObject());
            }
            else
            {
                result = await _jsInterop.DatabaseGetAsync(_path);
            }

            if (result.Success && result.Data != null)
            {
                var snapshot = ParseDataSnapshot<T>(result.Data);
                return Result<DataSnapshot<T>>.Success(snapshot);
            }

            return CreateFailureFromJsError<DataSnapshot<T>>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<DataSnapshot<T>>("database/unknown", ex.Message);
        }
    }

    /// <summary>
    /// Sets the data at this database location.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="value">
    /// The value to set at this location. Passing <c>null</c> will delete the data at this location,
    /// which is equivalent to calling <see cref="RemoveAsync"/>. This is standard Firebase behavior.
    /// </param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    public async Task<Result<Unit>> SetAsync<T>(T value)
    {
        try
        {
            var result = await _jsInterop.DatabaseSetAsync(_path, value!);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError<Unit>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<Unit>("database/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> UpdateAsync(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        try
        {
            var result = await _jsInterop.DatabaseUpdateAsync(_path, value);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError<Unit>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<Unit>("database/unknown", ex.Message);
        }
    }

    public async Task<Result<PushResult>> PushAsync<T>(T value)
    {
        try
        {
            var result = await _jsInterop.DatabasePushAsync(_path, value!);

            if (result.Success && result.Data != null)
            {
                var newRef = new WasmDatabaseReference(_jsInterop, $"{_path}/{result.Data.Key}");
                return Result<PushResult>.Success(new PushResult
                {
                    Key = result.Data.Key,
                    Reference = newRef
                });
            }

            return CreateFailureFromJsError<PushResult>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<PushResult>("database/unknown", ex.Message);
        }
    }

    public async Task<Result<Unit>> RemoveAsync()
    {
        try
        {
            var result = await _jsInterop.DatabaseRemoveAsync(_path);

            if (result.Success)
            {
                return Result<Unit>.Success(Unit.Value);
            }

            return CreateFailureFromJsError<Unit>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<Unit>("database/unknown", ex.Message);
        }
    }

    public async Task<Result<TransactionResult<T>>> TransactionAsync<T>(Func<T?, T?> updateFunction)
    {
        ArgumentNullException.ThrowIfNull(updateFunction);

        var handler = new DatabaseTransactionHandler<T>(updateFunction);
        var callbackRef = DotNetObjectReference.Create<ITransactionCallback>(handler);

        try
        {
            var result = await _jsInterop.DatabaseRunTransactionAsync(_path, callbackRef);

            if (result.Success && result.Data != null)
            {
                T? value = default;
                if (result.Data.Value.HasValue && result.Data.Value.Value.ValueKind != JsonValueKind.Null)
                {
                    value = DatabaseHelpers.DeserializeValue<T>(result.Data.Value.Value);
                }

                return Result<TransactionResult<T>>.Success(new TransactionResult<T>
                {
                    Committed = result.Data.Committed,
                    Value = value
                });
            }

            return CreateFailureFromJsError<TransactionResult<T>>(result.Error);
        }
        catch (Exception ex)
        {
            return CreateFailureResult<TransactionResult<T>>("database/unknown", ex.Message);
        }
        finally
        {
            callbackRef.Dispose();
        }
    }

    public IOnDisconnect OnDisconnect() => new WasmOnDisconnect(_jsInterop, _path);

    // Real-time Listeners

    public Action OnValue<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DatabaseValueSubscription<T>(
            _jsInterop, _path, _queryParams, onNext, onError);
        // Fire-and-forget with explicit discard - subscription errors are reported via onError callback
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    public Action OnChildAdded<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DatabaseChildSubscription<T>(
            _jsInterop, _path, _queryParams, "child_added", onNext, onError);
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    public Action OnChildChanged<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DatabaseChildSubscription<T>(
            _jsInterop, _path, _queryParams, "child_changed", onNext, onError);
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    public Action OnChildRemoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DatabaseChildSubscription<T>(
            _jsInterop, _path, _queryParams, "child_removed", onNext, onError);
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    public Action OnChildMoved<T>(Action<DataSnapshot<T>> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DatabaseChildSubscription<T>(
            _jsInterop, _path, _queryParams, "child_moved", onNext, onError);
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    // Helper Methods

    private static DataSnapshot<T> ParseDataSnapshot<T>(JsDataSnapshot jsSnapshot)
    {
        T? value = default;
        if (jsSnapshot.Exists && jsSnapshot.Value.HasValue)
        {
            value = DatabaseHelpers.DeserializeValue<T>(jsSnapshot.Value.Value);
        }

        return new DataSnapshot<T>
        {
            Key = jsSnapshot.Key ?? string.Empty,
            Exists = jsSnapshot.Exists,
            Value = value
        };
    }

    private static Result<T> CreateFailureFromJsError<T>(JsError? error)
    {
        var code = error?.Code ?? "database/unknown";
        var message = error?.Message ?? "Unknown error";
        var dbCode = DatabaseErrorCodeExtensions.FromFirebaseCode(code);
        return Result<T>.Failure(new FirebaseError(dbCode.ToFirebaseCode(), message));
    }

    private static Result<T> CreateFailureResult<T>(string code, string message)
    {
        var dbCode = DatabaseErrorCodeExtensions.FromFirebaseCode(code);
        return Result<T>.Failure(new FirebaseError(dbCode.ToFirebaseCode(), message));
    }

    private static string GetOrderByMethodName(string orderBy) => orderBy switch
    {
        "child" => "Child",
        "key" => "Key",
        "value" => "Value",
        _ => orderBy
    };
}

/// <summary>
/// Immutable record to hold query parameters for database queries.
/// </summary>
internal sealed record DatabaseQueryParams
{
    public string? OrderBy { get; init; }
    public string? OrderByPath { get; init; }
    public int? LimitToFirst { get; init; }
    public int? LimitToLast { get; init; }
    public object? StartAtValue { get; init; }
    public string? StartAtKey { get; init; }
    public object? EndAtValue { get; init; }
    public string? EndAtKey { get; init; }
    public object? EqualToValue { get; init; }
    public string? EqualToKey { get; init; }

    public bool HasQuery =>
        OrderBy != null ||
        LimitToFirst.HasValue ||
        LimitToLast.HasValue ||
        StartAtValue != null ||
        EndAtValue != null ||
        EqualToValue != null;

    public object ToJsObject() => new
    {
        orderBy = OrderBy,
        orderByPath = OrderByPath,
        limitToFirst = LimitToFirst,
        limitToLast = LimitToLast,
        startAtValue = StartAtValue,
        startAtKey = StartAtKey,
        endAtValue = EndAtValue,
        endAtKey = EndAtKey,
        equalToValue = EqualToValue,
        equalToKey = EqualToKey
    };
}
