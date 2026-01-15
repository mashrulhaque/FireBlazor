using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IRealtimeDatabase using JavaScript interop.
/// </summary>
internal sealed class WasmRealtimeDatabase : IRealtimeDatabase
{
    private readonly FirebaseJsInterop _jsInterop;

    public WasmRealtimeDatabase(FirebaseJsInterop jsInterop)
    {
        _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
    }

    public IDatabaseReference Ref(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new WasmDatabaseReference(_jsInterop, path);
    }

    public Action OnConnectionStateChanged(Action<bool> onConnected)
    {
        var subscription = new ConnectionStateSubscription(_jsInterop, onConnected);
        _ = subscription.StartAsync();
        return () => subscription.Dispose();
    }

    public async Task GoOfflineAsync()
    {
        await _jsInterop.DatabaseGoOfflineAsync();
    }

    public async Task GoOnlineAsync()
    {
        await _jsInterop.DatabaseGoOnlineAsync();
    }
}

internal sealed class ConnectionStateSubscription : IConnectionStateCallback, IDisposable
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly Action<bool> _onConnected;
    private readonly object _lock = new();
    private int _subscriptionId = -1;
    private DotNetObjectReference<IConnectionStateCallback>? _callbackRef;
    private bool _disposed;

    public ConnectionStateSubscription(FirebaseJsInterop jsInterop, Action<bool> onConnected)
    {
        _jsInterop = jsInterop;
        _onConnected = onConnected;
    }

    public async Task StartAsync()
    {
        _callbackRef = DotNetObjectReference.Create<IConnectionStateCallback>(this);
        var result = await _jsInterop.DatabaseSubscribeConnectionStateAsync(_callbackRef);

        if (result.Success && result.Data != null)
        {
            lock (_lock)
            {
                _subscriptionId = result.Data.SubscriptionId;
            }
        }
    }

    [JSInvokable]
    public void OnConnectionStateChanged(bool isConnected)
    {
        lock (_lock)
        {
            if (_disposed) return;
        }
        _onConnected(isConnected);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        if (_subscriptionId >= 0)
        {
            _ = _jsInterop.DatabaseUnsubscribeAsync(_subscriptionId);
        }
        _callbackRef?.Dispose();
    }
}
