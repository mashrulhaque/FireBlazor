using Microsoft.Extensions.Logging;

namespace FireBlazor;

/// <summary>
/// Configuration options for Firebase services.
/// </summary>
public sealed class FirebaseOptions
{
    public string? ProjectId { get; private set; }
    public string? ApiKey { get; private set; }
    public string? AuthDomain { get; private set; }
    public string? StorageBucket { get; private set; }
    public string? DatabaseUrl { get; private set; }
    public string? AppId { get; private set; }
    public string? MessagingSenderId { get; private set; }

    public LogLevel LogLevel { get; private set; } = LogLevel.Warning;
    public bool LogOperations { get; private set; }
    public bool LogSubscriptions { get; private set; }

    internal AuthOptions? AuthOptions { get; private set; }
    internal FirestoreOptions? FirestoreOptions { get; private set; }
    internal StorageOptions? StorageOptions { get; private set; }
    internal RealtimeDbOptions? RealtimeDbOptions { get; private set; }
    internal AppCheckOptions? AppCheckOptions { get; private set; }
    internal EmulatorOptions? EmulatorOptions { get; private set; }

    public FirebaseOptions WithProject(string projectId)
    {
        ProjectId = projectId;
        return this;
    }

    public FirebaseOptions WithApiKey(string apiKey)
    {
        ApiKey = apiKey;
        return this;
    }

    public FirebaseOptions WithAuthDomain(string authDomain)
    {
        AuthDomain = authDomain;
        return this;
    }

    public FirebaseOptions WithStorageBucket(string storageBucket)
    {
        StorageBucket = storageBucket;
        return this;
    }

    public FirebaseOptions WithDatabaseUrl(string databaseUrl)
    {
        DatabaseUrl = databaseUrl;
        return this;
    }

    public FirebaseOptions WithAppId(string appId)
    {
        AppId = appId;
        return this;
    }

    public FirebaseOptions WithMessagingSenderId(string senderId)
    {
        MessagingSenderId = senderId;
        return this;
    }

    public FirebaseOptions WithLogging(LogLevel logLevel = LogLevel.Debug)
    {
        LogLevel = logLevel;
        return this;
    }

    public FirebaseOptions WithLogging(Action<LoggingOptions> configure)
    {
        var loggingOptions = new LoggingOptions();
        configure(loggingOptions);
        LogLevel = loggingOptions.LogLevel;
        LogOperations = loggingOptions.LogOperations;
        LogSubscriptions = loggingOptions.LogSubscriptions;
        return this;
    }

    public FirebaseOptions UseAuth(Action<AuthOptions>? configure = null)
    {
        AuthOptions = new AuthOptions();
        configure?.Invoke(AuthOptions);
        return this;
    }

    public FirebaseOptions UseFirestore(Action<FirestoreOptions>? configure = null)
    {
        FirestoreOptions = new FirestoreOptions();
        configure?.Invoke(FirestoreOptions);
        return this;
    }

    public FirebaseOptions UseStorage(Action<StorageOptions>? configure = null)
    {
        StorageOptions = new StorageOptions();
        configure?.Invoke(StorageOptions);
        return this;
    }

    public FirebaseOptions UseRealtimeDatabase(Action<RealtimeDbOptions>? configure = null)
    {
        RealtimeDbOptions = new RealtimeDbOptions();
        configure?.Invoke(RealtimeDbOptions);
        return this;
    }

    public FirebaseOptions UseAppCheck(Action<AppCheckOptions>? configure = null)
    {
        AppCheckOptions = new AppCheckOptions();
        configure?.Invoke(AppCheckOptions);
        return this;
    }

    public FirebaseOptions UseEmulators(Action<EmulatorOptions> configure)
    {
        EmulatorOptions = new EmulatorOptions();
        configure(EmulatorOptions);
        return this;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectId))
            throw new InvalidOperationException("Firebase ProjectId is required. Call WithProject() or configure in appsettings.json.");
    }
}

public sealed class LoggingOptions
{
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;
    public bool LogOperations { get; set; }
    public bool LogSubscriptions { get; set; }
}

public sealed class AuthOptions
{
    public bool EmailPasswordEnabled { get; private set; }
    public string? GoogleClientId { get; private set; }
    public string? GitHubClientId { get; private set; }
    public string? MicrosoftClientId { get; private set; }
    public string? AppleClientId { get; private set; }
    public string? FacebookAppId { get; private set; }

    public AuthOptions EnableEmailPassword()
    {
        EmailPasswordEnabled = true;
        return this;
    }

    public AuthOptions EnableGoogle(string clientId)
    {
        GoogleClientId = clientId;
        return this;
    }

    public AuthOptions EnableGitHub(string clientId)
    {
        GitHubClientId = clientId;
        return this;
    }

    public AuthOptions EnableMicrosoft(string clientId)
    {
        MicrosoftClientId = clientId;
        return this;
    }

    public AuthOptions EnableApple(string clientId)
    {
        AppleClientId = clientId;
        return this;
    }

    public AuthOptions EnableFacebook(string appId)
    {
        FacebookAppId = appId;
        return this;
    }
}

public sealed class FirestoreOptions
{
    public bool OfflinePersistenceEnabled { get; private set; }

    public FirestoreOptions EnableOfflinePersistence()
    {
        OfflinePersistenceEnabled = true;
        return this;
    }
}

public sealed class StorageOptions
{
    /// <summary>Default maximum file size for browser uploads (50 MB).</summary>
    public const long DefaultMaxBrowserFileSize = 50 * 1024 * 1024;

    public string? CustomBucket { get; private set; }

    /// <summary>Maximum file size for browser file uploads. Defaults to 50 MB.</summary>
    public long MaxBrowserFileSize { get; private set; } = DefaultMaxBrowserFileSize;

    public StorageOptions WithBucket(string bucket)
    {
        CustomBucket = bucket;
        return this;
    }

    /// <summary>
    /// Sets the maximum file size for browser uploads.
    /// </summary>
    /// <param name="maxSize">Maximum size in bytes.</param>
    public StorageOptions WithMaxBrowserFileSize(long maxSize)
    {
        MaxBrowserFileSize = maxSize;
        return this;
    }
}

public sealed class RealtimeDbOptions
{
    public string? CustomUrl { get; private set; }

    public RealtimeDbOptions WithUrl(string url)
    {
        CustomUrl = url;
        return this;
    }
}

public sealed class AppCheckOptions
{
    private bool _validated;

    /// <summary>ReCaptcha V3 site key for App Check attestation.</summary>
    public string? ReCaptchaSiteKey { get; private set; }

    /// <summary>ReCaptcha Enterprise site key for App Check attestation.</summary>
    public string? ReCaptchaEnterpriseSiteKey { get; private set; }

    /// <summary>Whether debug mode is enabled for development.</summary>
    public bool DebugMode { get; private set; }

    /// <summary>
    /// Custom debug token to use in development. Must be registered in Firebase Console.
    /// If null and DebugMode is true, Firebase will auto-generate a token (logged to console).
    /// </summary>
    public string? DebugToken { get; private set; }

    /// <summary>Whether token auto-refresh is enabled. Defaults to true.</summary>
    public bool IsTokenAutoRefreshEnabled { get; private set; } = true;

    /// <summary>Whether to auto-detect localhost and enable debug mode.</summary>
    public bool AutoDetectDebugMode { get; private set; }

    /// <summary>Callback for custom token refresh logic.</summary>
    public Func<Task<string>>? OnTokenRefresh { get; set; }

    /// <summary>Configures App Check to use ReCaptcha V3 provider.</summary>
    /// <param name="siteKey">Your ReCaptcha V3 site key from Google reCAPTCHA admin.</param>
    public AppCheckOptions ReCaptchaV3(string siteKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siteKey, nameof(siteKey));

        if (ReCaptchaEnterpriseSiteKey != null)
            throw new InvalidOperationException(
                "Cannot use both ReCaptchaV3 and ReCaptchaEnterprise. Choose one provider.");

        ReCaptchaSiteKey = siteKey;
        return this;
    }

    /// <summary>Configures App Check to use ReCaptcha Enterprise provider.</summary>
    /// <param name="siteKey">Your ReCaptcha Enterprise site key.</param>
    public AppCheckOptions ReCaptchaEnterprise(string siteKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siteKey, nameof(siteKey));

        if (ReCaptchaSiteKey != null)
            throw new InvalidOperationException(
                "Cannot use both ReCaptchaV3 and ReCaptchaEnterprise. Choose one provider.");

        ReCaptchaEnterpriseSiteKey = siteKey;
        return this;
    }

    /// <summary>
    /// Enables automatic debug mode detection based on hostname.
    /// Debug mode is enabled when running on localhost, 127.0.0.1, or [::1].
    /// This is the recommended setting for development.
    /// </summary>
    public AppCheckOptions AutoDebug()
    {
        AutoDetectDebugMode = true;
        return this;
    }

    /// <summary>
    /// Enables debug mode for development. Firebase will auto-generate a debug token
    /// that appears in the browser console. Register this token in Firebase Console.
    /// </summary>
    public AppCheckOptions Debug()
    {
        DebugMode = true;
        return this;
    }

    /// <summary>
    /// Enables debug mode with a specific debug token.
    /// Use this when you have already registered a debug token in Firebase Console.
    /// </summary>
    /// <param name="token">The debug token registered in Firebase Console.</param>
    public AppCheckOptions WithDebugToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        if (!Guid.TryParse(token, out _))
            throw new ArgumentException(
                "Debug token must be a valid UUID format (e.g., '12345678-1234-1234-1234-123456789abc')",
                nameof(token));

        DebugMode = true;
        DebugToken = token;
        return this;
    }

    /// <summary>
    /// Sets whether token auto-refresh is enabled.
    /// When enabled, the SDK automatically refreshes App Check tokens as needed.
    /// </summary>
    /// <param name="enabled">Whether to enable auto-refresh. Defaults to true.</param>
    public AppCheckOptions TokenAutoRefresh(bool enabled)
    {
        IsTokenAutoRefreshEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Validates the App Check configuration.
    /// </summary>
    internal void Validate()
    {
        if (_validated) return;

        if (ReCaptchaSiteKey == null &&
            ReCaptchaEnterpriseSiteKey == null &&
            !AutoDetectDebugMode &&
            !DebugMode)
        {
            throw new InvalidOperationException(
                "App Check requires a provider. Call .ReCaptchaV3(key), .ReCaptchaEnterprise(key), .AutoDebug(), or .Debug()");
        }

        _validated = true;
    }
}

/// <summary>
/// Configuration options for connecting to Firebase Emulator Suite.
/// </summary>
public sealed class EmulatorOptions
{
    /// <summary>Default Firebase Auth emulator port.</summary>
    public const int DefaultAuthPort = 9099;
    /// <summary>Default Firestore emulator port.</summary>
    public const int DefaultFirestorePort = 8080;
    /// <summary>Default Storage emulator port.</summary>
    public const int DefaultStoragePort = 9199;
    /// <summary>Default Realtime Database emulator port.</summary>
    public const int DefaultRealtimeDatabasePort = 9000;

    /// <summary>Host and port for Auth emulator (e.g., "localhost:9099").</summary>
    public string? AuthHost { get; private set; }

    /// <summary>Host and port for Firestore emulator (e.g., "localhost:8080").</summary>
    public string? FirestoreHost { get; private set; }

    /// <summary>Host and port for Storage emulator (e.g., "localhost:9199").</summary>
    public string? StorageHost { get; private set; }

    /// <summary>Host and port for Realtime Database emulator (e.g., "localhost:9000").</summary>
    public string? RealtimeDatabaseHost { get; private set; }

    /// <summary>Whether any emulator is configured.</summary>
    public bool IsEnabled => AuthHost != null || FirestoreHost != null || StorageHost != null || RealtimeDatabaseHost != null;

    /// <summary>Whether Auth emulator is configured.</summary>
    public bool IsAuthEnabled => AuthHost != null;

    /// <summary>Whether Firestore emulator is configured.</summary>
    public bool IsFirestoreEnabled => FirestoreHost != null;

    /// <summary>Whether Storage emulator is configured.</summary>
    public bool IsStorageEnabled => StorageHost != null;

    /// <summary>Whether Realtime Database emulator is configured.</summary>
    public bool IsRealtimeDatabaseEnabled => RealtimeDatabaseHost != null;

    /// <summary>Configures Auth emulator connection.</summary>
    /// <param name="host">Host and port (e.g., "localhost:9099")</param>
    /// <exception cref="ArgumentException">Thrown when host format is invalid.</exception>
    public EmulatorOptions Auth(string host)
    {
        ValidateHostFormat(host, nameof(host));
        AuthHost = host;
        return this;
    }

    /// <summary>Configures Firestore emulator connection.</summary>
    /// <param name="host">Host and port (e.g., "localhost:8080")</param>
    /// <exception cref="ArgumentException">Thrown when host format is invalid.</exception>
    public EmulatorOptions Firestore(string host)
    {
        ValidateHostFormat(host, nameof(host));
        FirestoreHost = host;
        return this;
    }

    /// <summary>Configures Storage emulator connection.</summary>
    /// <param name="host">Host and port (e.g., "localhost:9199")</param>
    /// <exception cref="ArgumentException">Thrown when host format is invalid.</exception>
    public EmulatorOptions Storage(string host)
    {
        ValidateHostFormat(host, nameof(host));
        StorageHost = host;
        return this;
    }

    /// <summary>Configures Realtime Database emulator connection.</summary>
    /// <param name="host">Host and port (e.g., "localhost:9000")</param>
    /// <exception cref="ArgumentException">Thrown when host format is invalid.</exception>
    public EmulatorOptions RealtimeDatabase(string host)
    {
        ValidateHostFormat(host, nameof(host));
        RealtimeDatabaseHost = host;
        return this;
    }

    /// <summary>
    /// Configures all emulators on the specified host with default ports.
    /// This is intended for local development only.
    /// </summary>
    /// <param name="host">The host (e.g., "localhost" or "127.0.0.1")</param>
    /// <param name="authPort">Auth emulator port (default: 9099)</param>
    /// <param name="firestorePort">Firestore emulator port (default: 8080)</param>
    /// <param name="storagePort">Storage emulator port (default: 9199)</param>
    /// <param name="databasePort">Realtime Database emulator port (default: 9000)</param>
    /// <exception cref="ArgumentException">Thrown when host is null or whitespace.</exception>
    public EmulatorOptions All(
        string host,
        int authPort = DefaultAuthPort,
        int firestorePort = DefaultFirestorePort,
        int storagePort = DefaultStoragePort,
        int databasePort = DefaultRealtimeDatabasePort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host, nameof(host));
        AuthHost = $"{host}:{authPort}";
        FirestoreHost = $"{host}:{firestorePort}";
        StorageHost = $"{host}:{storagePort}";
        RealtimeDatabaseHost = $"{host}:{databasePort}";
        return this;
    }

    private static void ValidateHostFormat(string hostPort, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPort, paramName);
        var parts = hostPort.Split(':');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[1], out _))
        {
            throw new ArgumentException($"Invalid host format. Expected 'hostname:port' (e.g., 'localhost:9099').", paramName);
        }
    }

    /// <summary>
    /// Parses the Auth host into host and port components.
    /// </summary>
    /// <returns>Tuple of (host, port) or null if not configured.</returns>
    public (string Host, int Port)? GetAuthHostAndPort() => ParseHostAndPort(AuthHost);

    /// <summary>
    /// Parses the Firestore host into host and port components.
    /// </summary>
    public (string Host, int Port)? GetFirestoreHostAndPort() => ParseHostAndPort(FirestoreHost);

    /// <summary>
    /// Parses the Storage host into host and port components.
    /// </summary>
    public (string Host, int Port)? GetStorageHostAndPort() => ParseHostAndPort(StorageHost);

    /// <summary>
    /// Parses the Realtime Database host into host and port components.
    /// </summary>
    public (string Host, int Port)? GetRealtimeDatabaseHostAndPort() => ParseHostAndPort(RealtimeDatabaseHost);

    private static (string Host, int Port)? ParseHostAndPort(string? hostPort)
    {
        if (string.IsNullOrEmpty(hostPort))
            return null;

        var parts = hostPort.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            return null;

        return (parts[0], port);
    }
}
