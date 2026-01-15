using System.Diagnostics;
using System.Text.Json;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IDocumentReference using JavaScript interop.
/// </summary>
internal sealed class WasmDocumentReference<T> : IDocumentReference<T> where T : class
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;

    public WasmDocumentReference(FirebaseJsInterop jsInterop, string path)
    {
        _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public string Id => _path.Contains('/') ? _path[(LastSlashIndex + 1)..] : _path;

    public string Path => _path;

    private int LastSlashIndex => _path.LastIndexOf('/');

    public ICollectionReference<TChild> Collection<TChild>(string path) where TChild : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new WasmCollectionReference<TChild>(_jsInterop, $"{_path}/{path}");
    }

    public async Task<Result<DocumentSnapshot<T>>> GetAsync()
    {
        var result = await _jsInterop.FirestoreGetAsync(_path);

        if (!result.Success)
            return Result<DocumentSnapshot<T>>.Failure(
                new FirebaseError(result.Error!.Code, result.Error.Message));

        var snapshot = SnapshotParser.Parse<T>(result.Data, Id, _path);
        return Result<DocumentSnapshot<T>>.Success(snapshot);
    }

    public async Task<Result<Unit>> SetAsync(T data, bool merge = false)
    {
        ArgumentNullException.ThrowIfNull(data);

        var result = await _jsInterop.FirestoreSetAsync(_path, data, merge);

        if (!result.Success)
            return Result<Unit>.Failure(
                new FirebaseError(result.Error!.Code, result.Error.Message));

        return Unit.Value;
    }

    public async Task<Result<Unit>> UpdateAsync(object fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var result = await _jsInterop.FirestoreUpdateAsync(_path, fields);

        if (!result.Success)
            return Result<Unit>.Failure(
                new FirebaseError(result.Error!.Code, result.Error.Message));

        return Unit.Value;
    }

    public async Task<Result<Unit>> DeleteAsync()
    {
        var result = await _jsInterop.FirestoreDeleteAsync(_path);

        if (!result.Success)
            return Result<Unit>.Failure(
                new FirebaseError(result.Error!.Code, result.Error.Message));

        return Unit.Value;
    }

    public Action OnSnapshot(Action<DocumentSnapshot<T>?> onNext, Action<Exception>? onError = null)
    {
        var subscription = new DocumentSnapshotSubscription<T>(
            _jsInterop, _path, Id, onNext, onError);
        subscription.StartAsync().ConfigureAwait(false);
        return () => subscription.Dispose();
    }
}

/// <summary>
/// Manages a real-time subscription to a Firestore document.
/// Thread-safe and properly handles disposal during async startup.
/// </summary>
internal sealed class DocumentSnapshotSubscription<T> : ISnapshotCallback, IDisposable, IAsyncDisposable where T : class
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _path;
    private readonly string _id;
    private readonly Action<DocumentSnapshot<T>?> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly object _lock = new();
    private DotNetObjectReference<ISnapshotCallback>? _callbackRef;
    private int _subscriptionId;
    private bool _disposed;

    public DocumentSnapshotSubscription(
        FirebaseJsInterop jsInterop,
        string path,
        string id,
        Action<DocumentSnapshot<T>?> onNext,
        Action<Exception>? onError)
    {
        _jsInterop = jsInterop;
        _path = path;
        _id = id;
        _onNext = onNext;
        _onError = onError;
    }

    public async Task StartAsync()
    {
        DotNetObjectReference<ISnapshotCallback>? callbackRef;

        lock (_lock)
        {
            if (_disposed) return;
            callbackRef = DotNetObjectReference.Create<ISnapshotCallback>(this);
            _callbackRef = callbackRef;
        }

        try
        {
            var result = await _jsInterop.FirestoreSubscribeDocumentAsync(_path, callbackRef);

            lock (_lock)
            {
                // Check if disposed during await
                if (_disposed)
                {
                    // We were disposed while awaiting - clean up the subscription
                    if (result.Success && result.Data != null)
                    {
                        _ = UnsubscribeAsync(result.Data.SubscriptionId);
                    }
                    callbackRef.Dispose();
                    _callbackRef = null;
                    return;
                }

                if (result.Success && result.Data != null)
                {
                    _subscriptionId = result.Data.SubscriptionId;
                }
                else if (result.Error != null)
                {
                    // Dispose callback ref on error to prevent memory leak
                    callbackRef.Dispose();
                    _callbackRef = null;
                    _onError?.Invoke(new FirebaseException(result.Error.Code, result.Error.Message));
                }
            }
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                callbackRef.Dispose();
                _callbackRef = null;
            }
            _onError?.Invoke(ex);
        }
    }

    [JSInvokable]
    public void OnDocumentSnapshot(JsonElement data)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }

        var snapshot = SnapshotParser.Parse<T>(data, _id, _path);
        _onNext(snapshot.Exists ? snapshot : null);
    }

    [JSInvokable]
    public void OnCollectionSnapshot(JsonElement[] data)
    {
        // Not used for document subscriptions
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

    public void Dispose()
    {
        int subscriptionId;
        DotNetObjectReference<ISnapshotCallback>? callbackRef;

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
            _ = UnsubscribeAsync(subscriptionId);
        }

        callbackRef?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        int subscriptionId;
        DotNetObjectReference<ISnapshotCallback>? callbackRef;

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
            await UnsubscribeAsync(subscriptionId);
        }

        callbackRef?.Dispose();
    }

    private async Task UnsubscribeAsync(int subscriptionId)
    {
        try
        {
            var result = await _jsInterop.FirestoreUnsubscribeAsync(subscriptionId);
            if (!result.Success && result.Error != null)
            {
                Debug.WriteLine($"[FireBlazor] Failed to unsubscribe from document {_path}: {result.Error.Code} - {result.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FireBlazor] Error during document unsubscribe: {ex.Message}");
        }
    }
}
