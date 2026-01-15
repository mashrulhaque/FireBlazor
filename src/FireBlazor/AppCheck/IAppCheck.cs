namespace FireBlazor;

/// <summary>
/// Firebase App Check service interface.
/// App Check helps protect your backend resources from abuse, such as
/// billing fraud or phishing.
/// </summary>
public interface IAppCheck
{
    /// <summary>
    /// Gets the current App Check token.
    /// Returns null if App Check is not initialized or no token is available.
    /// </summary>
    AppCheckToken? CurrentToken { get; }

    /// <summary>
    /// Whether App Check has been activated.
    /// </summary>
    bool IsActivated { get; }

    /// <summary>
    /// Event raised when the App Check token changes.
    /// </summary>
    event Action<AppCheckToken?>? OnTokenChanged;

    /// <summary>
    /// Gets a valid App Check token.
    /// This will return a cached token if one is available and not expired,
    /// or fetch a new token if necessary.
    /// </summary>
    /// <param name="forceRefresh">If true, forces a refresh even if a valid token exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<AppCheckToken>> GetTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates App Check with the configured provider.
    /// Must be called before using other Firebase services that require App Check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<Unit>> ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to token change events and returns a disposable to unsubscribe.
    /// This is the recommended way to listen for token changes as it ensures proper cleanup.
    /// </summary>
    /// <param name="callback">Callback invoked when the token changes.</param>
    /// <returns>A disposable that unsubscribes when disposed.</returns>
    IDisposable SubscribeToTokenChanges(Action<AppCheckToken?> callback);

    /// <summary>
    /// Gets the current status of App Check.
    /// </summary>
    AppCheckStatus Status { get; }

    /// <summary>
    /// Event raised when the App Check status changes.
    /// </summary>
    event Action<AppCheckStatus>? OnStatusChanged;

    /// <summary>
    /// Gets the last error that occurred, if Status is Failed.
    /// Returns null when Status is not Failed.
    /// </summary>
    FirebaseError? LastError { get; }

    /// <summary>
    /// Sets whether App Check token auto-refresh is enabled.
    /// Note: In Firebase JS SDK v9+, this setting is configured during initialization
    /// and cannot be changed at runtime. This method exists for interface compatibility.
    /// </summary>
    [Obsolete("Token auto-refresh is configured during initialization via AppCheckOptions.TokenAutoRefresh(). This method has no effect at runtime.")]
    void SetTokenAutoRefreshEnabled(bool enabled);
}

/// <summary>
/// Represents a Firebase App Check token.
/// </summary>
public sealed class AppCheckToken
{
    /// <summary>
    /// The App Check token string.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// The timestamp (in milliseconds since epoch) when the token expires.
    /// </summary>
    public required long ExpireTimeMillis { get; init; }

    /// <summary>
    /// Gets the expiration time as a DateTimeOffset.
    /// </summary>
    public DateTimeOffset ExpirationTime =>
        DateTimeOffset.FromUnixTimeMilliseconds(ExpireTimeMillis);

    /// <summary>
    /// Returns true if the token has expired.
    /// </summary>
    public bool IsExpired =>
        DateTimeOffset.UtcNow >= ExpirationTime;
}
