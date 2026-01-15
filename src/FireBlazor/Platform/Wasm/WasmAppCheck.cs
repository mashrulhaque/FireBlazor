using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WASM implementation of Firebase App Check using JavaScript interop.
/// </summary>
/// <remarks>
/// This implementation is designed for Blazor WebAssembly's single-threaded execution model.
/// All operations are performed on the main thread via JavaScript interop.
/// </remarks>
internal sealed class WasmAppCheck : IAppCheck, IAsyncDisposable
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly AppCheckOptions? _options;
    private AppCheckToken? _currentToken;
    private bool _initialized;
    private bool _disposed;
    private readonly List<AppCheckTokenSubscription> _subscriptions = [];
    private int? _jsSubscriptionId;
    private DotNetObjectReference<AppCheckTokenCallbackHandler>? _callbackRef;
    private AppCheckStatus _status = AppCheckStatus.NotInitialized;
    private FirebaseError? _lastError;

    public WasmAppCheck(FirebaseJsInterop jsInterop, AppCheckOptions? options = null)
    {
        _jsInterop = jsInterop;
        _options = options;
    }

    public AppCheckToken? CurrentToken => _currentToken;

    public bool IsActivated => _initialized;

    public event Action<AppCheckToken?>? OnTokenChanged;

    public AppCheckStatus Status => _status;

    public event Action<AppCheckStatus>? OnStatusChanged;

    public FirebaseError? LastError => _lastError;

    private void SetStatus(AppCheckStatus status, FirebaseError? error = null)
    {
        _status = status;
        _lastError = status == AppCheckStatus.Failed ? error : null;
        OnStatusChanged?.Invoke(status);
    }

    public async Task<Result<Unit>> ActivateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Initialize App Check with options if not already initialized
            if (!_initialized)
            {
                SetStatus(AppCheckStatus.Initializing);

                await _jsInterop.InitializeAppCheckAsync(_options);
                _initialized = true;

                // Set up the JS subscription for token changes
                await SetupJsTokenSubscriptionAsync();
            }

            var result = await _jsInterop.AppCheckActivateAsync();
            if (result.Success)
            {
                SetStatus(AppCheckStatus.Active);
                return Result<Unit>.Success(Unit.Value);
            }

            var error = result.Error ?? new JsError { Code = "appCheck/unknown", Message = "Unknown error" };
            var firebaseError = new FirebaseError(error.Code, error.Message);
            SetStatus(AppCheckStatus.Failed, firebaseError);
            return Result<Unit>.Failure(firebaseError);
        }
        catch (OperationCanceledException)
        {
            var error = new FirebaseError("appCheck/cancelled", "Operation was cancelled");
            SetStatus(AppCheckStatus.Failed, error);
            return Result<Unit>.Failure(error);
        }
        catch (Exception ex)
        {
            var error = new FirebaseError("appCheck/unknown", ex.Message);
            SetStatus(AppCheckStatus.Failed, error);
            return Result<Unit>.Failure(error);
        }
    }

    public async Task<Result<AppCheckToken>> GetTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var previousStatus = _status;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Return cached token if valid and not forcing refresh
            if (!forceRefresh && _currentToken != null && !_currentToken.IsExpired)
            {
                return Result<AppCheckToken>.Success(_currentToken);
            }

            // Set TokenRefreshing status if currently Active
            if (_status == AppCheckStatus.Active)
            {
                SetStatus(AppCheckStatus.TokenRefreshing);
            }

            var result = await _jsInterop.AppCheckGetTokenAsync(forceRefresh);

            if (result.Success && result.Data != null)
            {
                _currentToken = new AppCheckToken
                {
                    Token = result.Data.Token,
                    ExpireTimeMillis = result.Data.ExpireTimeMillis
                };

                SetStatus(AppCheckStatus.Active);

                // Notify subscribers about the new token
                NotifyTokenChanged(_currentToken);
                return Result<AppCheckToken>.Success(_currentToken);
            }

            var error = result.Error ?? new JsError { Code = "appCheck/unknown", Message = "Unknown error" };
            // Restore previous status on failure
            SetStatus(previousStatus);
            return Result<AppCheckToken>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            // Restore previous status on failure
            SetStatus(previousStatus);
            return Result<AppCheckToken>.Failure(new FirebaseError("appCheck/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            // Restore previous status on failure
            SetStatus(previousStatus);
            return Result<AppCheckToken>.Failure(new FirebaseError("appCheck/unknown", ex.Message));
        }
    }

    public IDisposable SubscribeToTokenChanges(Action<AppCheckToken?> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var subscription = new AppCheckTokenSubscription(this, callback);
        _subscriptions.Add(subscription);
        return subscription;
    }

    [Obsolete("Token auto-refresh is configured during initialization via AppCheckOptions.TokenAutoRefresh(). This method has no effect at runtime.")]
    public void SetTokenAutoRefreshEnabled(bool enabled)
    {
        // No-op: In Firebase JS SDK v9+, token auto-refresh is configured during initialization
        // and cannot be changed at runtime. This method is provided for interface compatibility.
    }

    private async Task SetupJsTokenSubscriptionAsync()
    {
        if (_callbackRef != null) return;

        var handler = new AppCheckTokenCallbackHandler(this);
        _callbackRef = DotNetObjectReference.Create(handler);

        var result = await _jsInterop.AppCheckSubscribeTokenChangedAsync(_callbackRef);
        if (result.Success && result.Data != null)
        {
            _jsSubscriptionId = result.Data.SubscriptionId;
        }
    }

    internal void HandleTokenChanged(JsAppCheckToken jsToken)
    {
        _currentToken = new AppCheckToken
        {
            Token = jsToken.Token,
            ExpireTimeMillis = jsToken.ExpireTimeMillis
        };
        NotifyTokenChanged(_currentToken);
    }

    private void NotifyTokenChanged(AppCheckToken? token)
    {
        OnTokenChanged?.Invoke(token);
        foreach (var subscription in _subscriptions)
        {
            subscription.Invoke(token);
        }
    }

    internal void RemoveSubscription(AppCheckTokenSubscription subscription)
    {
        _subscriptions.Remove(subscription);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe from JS token changes
        if (_jsSubscriptionId.HasValue)
        {
            await _jsInterop.AppCheckUnsubscribeTokenChangedAsync(_jsSubscriptionId.Value);
        }

        // Dispose callback reference
        _callbackRef?.Dispose();

        // Clear subscriptions
        _subscriptions.Clear();
    }
}

/// <summary>
/// Callback handler for App Check token changes from JavaScript.
/// </summary>
internal sealed class AppCheckTokenCallbackHandler : IAppCheckTokenCallback
{
    private readonly WasmAppCheck _appCheck;

    public AppCheckTokenCallbackHandler(WasmAppCheck appCheck)
    {
        _appCheck = appCheck;
    }

    [JSInvokable]
    public void OnTokenChanged(JsAppCheckToken token)
    {
        _appCheck.HandleTokenChanged(token);
    }
}

/// <summary>
/// Represents a subscription to App Check token changes.
/// </summary>
internal sealed class AppCheckTokenSubscription : IDisposable
{
    private readonly WasmAppCheck _appCheck;
    private readonly Action<AppCheckToken?> _callback;
    private bool _disposed;

    public AppCheckTokenSubscription(WasmAppCheck appCheck, Action<AppCheckToken?> callback)
    {
        _appCheck = appCheck;
        _callback = callback;
    }

    public void Invoke(AppCheckToken? token)
    {
        if (!_disposed)
        {
            _callback(token);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _appCheck.RemoveSubscription(this);
    }
}
