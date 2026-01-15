namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IAppCheck for testing.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FakeAppCheck : IAppCheck
{
    private AppCheckToken? _currentToken;
    private AppCheckToken? _configuredToken;
    private FirebaseError? _simulatedError;
    private bool _autoRefreshEnabled = true;
    private bool _isActivated;
    private AppCheckStatus _status = AppCheckStatus.NotInitialized;
    private FirebaseError? _lastError;
    private readonly List<Action<AppCheckToken?>> _subscriptions = [];

    public AppCheckToken? CurrentToken => _currentToken;

    public bool IsActivated => _isActivated;

    public AppCheckStatus Status => _status;

    public FirebaseError? LastError => _lastError;

    public event Action<AppCheckToken?>? OnTokenChanged;

    public event Action<AppCheckStatus>? OnStatusChanged;

    /// <summary>
    /// Configures a specific token to return.
    /// </summary>
    public void ConfigureToken(AppCheckToken token)
    {
        _configuredToken = token;
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Simulates a status change for testing.
    /// </summary>
    public void SimulateStatus(AppCheckStatus status, FirebaseError? error = null)
    {
        _status = status;
        _lastError = status == AppCheckStatus.Failed ? error : null;
        OnStatusChanged?.Invoke(status);
    }

    /// <summary>
    /// Resets all state to initial values.
    /// </summary>
    public void Reset()
    {
        _currentToken = null;
        _configuredToken = null;
        _simulatedError = null;
        _autoRefreshEnabled = true;
        _isActivated = false;
        _status = AppCheckStatus.NotInitialized;
        _lastError = null;
        _subscriptions.Clear();
    }

    public Task<Result<AppCheckToken>> GetTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<AppCheckToken>.Failure(new FirebaseError("appCheck/cancelled", "Operation was cancelled")));

        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<AppCheckToken>.Failure(error!));

        if (!forceRefresh && _currentToken != null && !_currentToken.IsExpired)
        {
            return Task.FromResult(Result<AppCheckToken>.Success(_currentToken));
        }

        var token = _configuredToken ?? GenerateToken();
        _currentToken = token;
        NotifyTokenChanged(_currentToken);
        return Task.FromResult(Result<AppCheckToken>.Success(token));
    }

    public Task<Result<Unit>> ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<Unit>.Failure(new FirebaseError("appCheck/cancelled", "Operation was cancelled")));

        if (TryConsumeSimulatedError(out var error))
        {
            _status = AppCheckStatus.Failed;
            _lastError = error;
            OnStatusChanged?.Invoke(_status);
            return Task.FromResult(Result<Unit>.Failure(error!));
        }

        _isActivated = true;
        _status = AppCheckStatus.Active;
        _lastError = null;
        OnStatusChanged?.Invoke(_status);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public IDisposable SubscribeToTokenChanges(Action<AppCheckToken?> callback)
    {
        _subscriptions.Add(callback);
        return new FakeAppCheckSubscription(this, callback);
    }

    [Obsolete("Token auto-refresh is configured during initialization via AppCheckOptions.TokenAutoRefresh(). This method has no effect at runtime.")]
    public void SetTokenAutoRefreshEnabled(bool enabled)
    {
        _autoRefreshEnabled = enabled;
    }

    /// <summary>
    /// Manually triggers a token change notification for testing.
    /// </summary>
    public void TriggerTokenChange(AppCheckToken? token)
    {
        _currentToken = token;
        NotifyTokenChanged(token);
    }

    private void NotifyTokenChanged(AppCheckToken? token)
    {
        OnTokenChanged?.Invoke(token);
        foreach (var subscription in _subscriptions)
        {
            subscription(token);
        }
    }

    internal void RemoveSubscription(Action<AppCheckToken?> callback)
    {
        _subscriptions.Remove(callback);
    }

    private static AppCheckToken GenerateToken()
    {
        return new AppCheckToken
        {
            Token = $"fake-app-check-token-{Guid.NewGuid():N}",
            ExpireTimeMillis = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds()
        };
    }

    private bool TryConsumeSimulatedError(out FirebaseError? error)
    {
        error = _simulatedError;
        _simulatedError = null;
        return error != null;
    }

    private sealed class FakeAppCheckSubscription : IDisposable
    {
        private readonly FakeAppCheck _appCheck;
        private readonly Action<AppCheckToken?> _callback;
        private bool _disposed;

        public FakeAppCheckSubscription(FakeAppCheck appCheck, Action<AppCheckToken?> callback)
        {
            _appCheck = appCheck;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _appCheck.RemoveSubscription(_callback);
        }
    }
}
