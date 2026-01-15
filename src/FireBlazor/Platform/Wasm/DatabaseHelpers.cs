using System.Diagnostics;
using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// Helper utilities for Realtime Database operations.
/// </summary>
internal static class DatabaseHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Deserializes a JsonElement value to the target type.
    /// Returns default(T) if deserialization fails.
    /// </summary>
    public static T? DeserializeValue<T>(JsonElement element)
    {
        try
        {
            return element.Deserialize<T>(JsonOptions);
        }
        catch (JsonException ex)
        {
            // Log at debug level - this is expected when data doesn't match the expected type
            Debug.WriteLine($"[FireBlazor] Failed to deserialize database value to {typeof(T).Name}: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Deserializes a JsonElement value to the target type.
    /// Returns success status and any exception that occurred.
    /// </summary>
    public static bool TryDeserializeValue<T>(JsonElement element, out T? value, out Exception? exception)
    {
        try
        {
            value = element.Deserialize<T>(JsonOptions);
            exception = null;
            return true;
        }
        catch (JsonException ex)
        {
            value = default;
            exception = ex;
            return false;
        }
    }
}

/// <summary>
/// Subscription handler for database OnValue listener.
/// </summary>
internal sealed class DatabaseValueSubscription<T> : IDatabaseSnapshotCallback, IDisposable
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly DatabaseQueryParams _queryParams;
    private readonly Action<DataSnapshot<T>> _onNext;
    private readonly Action<Exception>? _onError;

    private DotNetObjectReference<IDatabaseSnapshotCallback>? _callbackRef;
    private int _subscriptionId;
    private readonly object _lock = new();
    private bool _disposed;

    public DatabaseValueSubscription(
        FirebaseJsInterop jsInterop,
        string path,
        DatabaseQueryParams queryParams,
        Action<DataSnapshot<T>> onNext,
        Action<Exception>? onError)
    {
        _jsInterop = jsInterop;
        _path = path;
        _queryParams = queryParams;
        _onNext = onNext;
        _onError = onError;
    }

    public async Task StartAsync()
    {
        DotNetObjectReference<IDatabaseSnapshotCallback>? callbackRef;
        lock (_lock)
        {
            if (_disposed) return;
            _callbackRef = DotNetObjectReference.Create<IDatabaseSnapshotCallback>(this);
            callbackRef = _callbackRef;
        }

        try
        {
            var result = await _jsInterop.DatabaseSubscribeValueAsync(
                _path,
                _queryParams.HasQuery ? _queryParams.ToJsObject() : null,
                callbackRef);

            if (result.Success && result.Data != null)
            {
                lock (_lock)
                {
                    _subscriptionId = result.Data.SubscriptionId;
                }
            }
            else if (!result.Success && result.Error != null)
            {
                _onError?.Invoke(new FirebaseException(result.Error.Code, result.Error.Message));
            }
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
    }

    [JSInvokable]
    public void OnDataSnapshot(JsonElement data)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }

        try
        {
            var snapshot = ParseSnapshot(data);
            _onNext(snapshot);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
    }

    [JSInvokable]
    public void OnSnapshotError(JsError error)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }

        _onError?.Invoke(new FirebaseException(error.Code, error.Message));
    }

    private DataSnapshot<T> ParseSnapshot(JsonElement data)
    {
        var key = data.TryGetProperty("key", out var keyProp) ? keyProp.GetString() ?? "" : "";
        var exists = data.TryGetProperty("exists", out var existsProp) && existsProp.GetBoolean();

        T? value = default;
        if (exists && data.TryGetProperty("value", out var valueProp))
        {
            value = DatabaseHelpers.DeserializeValue<T>(valueProp);
        }

        return new DataSnapshot<T>
        {
            Key = key,
            Exists = exists,
            Value = value
        };
    }

    public void Dispose()
    {
        int subscriptionId;
        DotNetObjectReference<IDatabaseSnapshotCallback>? callbackRef;

        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            subscriptionId = _subscriptionId;
            callbackRef = _callbackRef;
            _callbackRef = null;
        }

        if (subscriptionId > 0)
        {
            _ = _jsInterop.DatabaseUnsubscribeAsync(subscriptionId);
        }

        callbackRef?.Dispose();
    }
}

/// <summary>
/// Subscription handler for database child event listeners (OnChildAdded, OnChildChanged, OnChildRemoved).
/// </summary>
internal sealed class DatabaseChildSubscription<T> : IDatabaseSnapshotCallback, IDisposable
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly DatabaseQueryParams _queryParams;
    private readonly string _eventType;
    private readonly Action<DataSnapshot<T>> _onNext;
    private readonly Action<Exception>? _onError;

    private DotNetObjectReference<IDatabaseSnapshotCallback>? _callbackRef;
    private int _subscriptionId;
    private readonly object _lock = new();
    private bool _disposed;

    public DatabaseChildSubscription(
        FirebaseJsInterop jsInterop,
        string path,
        DatabaseQueryParams queryParams,
        string eventType,
        Action<DataSnapshot<T>> onNext,
        Action<Exception>? onError)
    {
        _jsInterop = jsInterop;
        _path = path;
        _queryParams = queryParams;
        _eventType = eventType;
        _onNext = onNext;
        _onError = onError;
    }

    public async Task StartAsync()
    {
        DotNetObjectReference<IDatabaseSnapshotCallback>? callbackRef;
        lock (_lock)
        {
            if (_disposed) return;
            _callbackRef = DotNetObjectReference.Create<IDatabaseSnapshotCallback>(this);
            callbackRef = _callbackRef;
        }

        try
        {
            var result = await _jsInterop.DatabaseSubscribeChildAsync(
                _path,
                _eventType,
                _queryParams.HasQuery ? _queryParams.ToJsObject() : null,
                callbackRef);

            if (result.Success && result.Data != null)
            {
                lock (_lock)
                {
                    _subscriptionId = result.Data.SubscriptionId;
                }
            }
            else if (!result.Success && result.Error != null)
            {
                _onError?.Invoke(new FirebaseException(result.Error.Code, result.Error.Message));
            }
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
    }

    [JSInvokable]
    public void OnDataSnapshot(JsonElement data)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }

        try
        {
            var snapshot = ParseSnapshot(data);
            _onNext(snapshot);
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
    }

    [JSInvokable]
    public void OnSnapshotError(JsError error)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }

        _onError?.Invoke(new FirebaseException(error.Code, error.Message));
    }

    private DataSnapshot<T> ParseSnapshot(JsonElement data)
    {
        var key = data.TryGetProperty("key", out var keyProp) ? keyProp.GetString() ?? "" : "";
        var exists = data.TryGetProperty("exists", out var existsProp) && existsProp.GetBoolean();

        T? value = default;
        if (exists && data.TryGetProperty("value", out var valueProp))
        {
            value = DatabaseHelpers.DeserializeValue<T>(valueProp);
        }

        return new DataSnapshot<T>
        {
            Key = key,
            Exists = exists,
            Value = value
        };
    }

    public void Dispose()
    {
        int subscriptionId;
        DotNetObjectReference<IDatabaseSnapshotCallback>? callbackRef;

        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            subscriptionId = _subscriptionId;
            callbackRef = _callbackRef;
            _callbackRef = null;
        }

        if (subscriptionId > 0)
        {
            _ = _jsInterop.DatabaseUnsubscribeAsync(subscriptionId);
        }

        callbackRef?.Dispose();
    }
}
