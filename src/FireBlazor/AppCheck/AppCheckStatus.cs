namespace FireBlazor;

/// <summary>
/// Represents the current state of Firebase App Check.
/// </summary>
public enum AppCheckStatus
{
    /// <summary>App Check has not been initialized.</summary>
    NotInitialized,

    /// <summary>App Check is currently initializing.</summary>
    Initializing,

    /// <summary>App Check is active and ready.</summary>
    Active,

    /// <summary>App Check initialization or token refresh failed.</summary>
    Failed,

    /// <summary>App Check is refreshing the token in the background.</summary>
    TokenRefreshing
}
