using FireBlazor.Platform.Wasm;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FireBlazor;

/// <summary>
/// Default implementation of IFirebase gateway.
/// </summary>
internal sealed class Firebase : IFirebase
{
    private readonly FirebaseOptions _options;
    private readonly ILogger<Firebase> _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly FirebaseJsInterop _jsInterop;

    private IFirebaseAuth? _auth;
    private IFirestore? _firestore;
    private IFirebaseStorage? _storage;
    private IRealtimeDatabase? _realtimeDb;
    private IAppCheck? _appCheck;
    private IFirebaseAI? _ai;

    private bool _initialized;

    public Firebase(FirebaseOptions options, ILogger<Firebase> logger, IJSRuntime jsRuntime)
    {
        _options = options;
        _logger = logger;
        _jsRuntime = jsRuntime;
        _jsInterop = new FirebaseJsInterop(_jsRuntime);

        _options.Validate();
        _logger.LogDebug("Firebase initialized for project: {ProjectId}", _options.ProjectId);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        var emulators = _options.EmulatorOptions;

        // Initialize Firebase app
        await _jsInterop.InitializeAsync(_options);
        _logger.LogDebug("Firebase app initialized");

        // Initialize Auth if configured
        if (_options.AuthOptions != null)
        {
            await _jsInterop.InitializeAuthAsync(emulators?.AuthHost);
            _logger.LogDebug("Auth initialized{Emulator}", emulators?.IsAuthEnabled == true ? " (emulator)" : "");
        }

        // Initialize Firestore if configured
        if (_options.FirestoreOptions != null)
        {
            await _jsInterop.InitializeFirestoreAsync(_options.FirestoreOptions, emulators?.FirestoreHost);
            _logger.LogDebug("Firestore initialized{Emulator}", emulators?.IsFirestoreEnabled == true ? " (emulator)" : "");
        }

        // Initialize Storage if configured
        if (_options.StorageOptions != null)
        {
            await _jsInterop.InitializeStorageAsync(emulators?.StorageHost);
            _logger.LogDebug("Storage initialized{Emulator}", emulators?.IsStorageEnabled == true ? " (emulator)" : "");
        }

        // Initialize Realtime Database if configured
        if (_options.RealtimeDbOptions != null)
        {
            await _jsInterop.InitializeDatabaseAsync(_options.RealtimeDbOptions.CustomUrl, emulators?.RealtimeDatabaseHost);
            _logger.LogDebug("Realtime Database initialized{Emulator}", emulators?.IsRealtimeDatabaseEnabled == true ? " (emulator)" : "");
        }

        // Initialize App Check if configured
        if (_options.AppCheckOptions != null)
        {
            _options.AppCheckOptions.Validate();  // Fail fast on invalid config

            // Create the service early so Status is observable
            var appCheck = new WasmAppCheck(_jsInterop, _options.AppCheckOptions);
            _appCheck = appCheck;

            // Auto-activate
            var result = await appCheck.ActivateAsync();

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "App Check activation failed: {Error}. Firebase services may reject requests if enforcement is enabled.",
                    result.Error?.Message);
                // Don't throw - app continues but AppCheck.Status will be Failed
            }
            else
            {
                _logger.LogDebug("App Check activated successfully");
            }
        }

        _initialized = true;
        _logger.LogInformation("Firebase fully initialized for project: {ProjectId}", _options.ProjectId);
    }

    public IFirebaseAuth Auth => _auth ??= CreateAuthService();
    public IFirestore Firestore => _firestore ??= CreateFirestoreService();
    public IFirebaseStorage Storage => _storage ??= CreateStorageService();
    public IRealtimeDatabase RealtimeDb => _realtimeDb ??= CreateRealtimeDbService();
    public IAppCheck AppCheck => _appCheck ??= CreateAppCheckService();
    public IFirebaseAI AI => _ai ??= CreateAIService();

    public event Action<FirebaseOperation>? OnOperation;
    public event Action<FirebaseException>? OnError;

    internal void RaiseOperation(FirebaseOperation operation)
    {
        OnOperation?.Invoke(operation);
        if (_options.LogOperations)
            _logger.LogDebug("[{Service}] {Action} completed in {Duration}ms",
                operation.Service, operation.Action, operation.Duration.TotalMilliseconds);
    }

    internal void RaiseError(FirebaseException error)
    {
        OnError?.Invoke(error);
        _logger.LogError(error, "[{Code}] {Message}", error.Code, error.Message);
    }

    private IFirebaseAuth CreateAuthService()
    {
        return new WasmFirebaseAuth(_jsInterop);
    }

    private IFirestore CreateFirestoreService()
    {
        return new WasmFirestore(_jsInterop);
    }

    private IFirebaseStorage CreateStorageService()
    {
        return new WasmFirebaseStorage(_jsInterop);
    }

    private IRealtimeDatabase CreateRealtimeDbService()
    {
        return new WasmRealtimeDatabase(_jsInterop);
    }

    private IAppCheck CreateAppCheckService()
    {
        return new WasmAppCheck(_jsInterop, _options.AppCheckOptions);
    }

    private IFirebaseAI CreateAIService()
    {
        return new WasmFirebaseAI(_jsInterop);
    }
}
