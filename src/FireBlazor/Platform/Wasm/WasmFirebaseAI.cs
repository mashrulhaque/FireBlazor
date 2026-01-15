namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WASM implementation of Firebase AI Logic using JavaScript interop.
/// </summary>
internal sealed class WasmFirebaseAI : IFirebaseAI
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly Dictionary<string, WasmGenerativeModel> _models = new();
    private bool _initialized;

    public WasmFirebaseAI(FirebaseJsInterop jsInterop)
    {
        _jsInterop = jsInterop;
    }

    /// <summary>
    /// Initializes the Firebase AI service with the specified backend.
    /// </summary>
    /// <param name="backend">The backend to use ("google" or "vertex").</param>
    internal async Task InitializeAsync(string backend = "google")
    {
        if (_initialized)
            return;

        await _jsInterop.InitializeAIAsync(backend);
        _initialized = true;
    }

    public IGenerativeModel GetModel(string modelName, GenerationConfig? config = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName, nameof(modelName));

        // Ensure AI is initialized (lazy initialization)
        if (!_initialized)
        {
            // Fire and forget - actual initialization happens on first model use
            _ = InitializeAsync();
        }

        // Create a cache key that includes config to allow different configs for same model
        var cacheKey = config != null
            ? $"{modelName}_{config.GetHashCode()}"
            : modelName;

        if (!_models.TryGetValue(cacheKey, out var model))
        {
            model = new WasmGenerativeModel(_jsInterop, modelName, config);
            _models[cacheKey] = model;
        }

        return model;
    }

    public IImageModel GetImageModel(string modelName = "imagen-4.0-generate-001")
    {
        return new WasmImageModel(_jsInterop, modelName);
    }
}
