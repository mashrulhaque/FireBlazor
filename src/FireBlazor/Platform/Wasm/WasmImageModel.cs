namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WASM implementation of IImageModel using JavaScript interop.
/// </summary>
internal sealed class WasmImageModel : IImageModel
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _modelName;
    private bool _initialized;

    public WasmImageModel(FirebaseJsInterop jsInterop, string modelName)
    {
        _jsInterop = jsInterop;
        _modelName = modelName;
    }

    public string ModelName => _modelName;

    internal async Task<Result<Unit>> InitializeAsync()
    {
        if (_initialized)
            return Result<Unit>.Success(Unit.Value);

        var result = await _jsInterop.AIGetImageModelAsync(_modelName);

        if (result.Success)
        {
            _initialized = true;
            return Result<Unit>.Success(Unit.Value);
        }

        var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
        return Result<Unit>.Failure(new FirebaseError(error.Code, error.Message));
    }

    public async Task<Result<ImageGenerationResponse>> GenerateImagesAsync(
        string prompt,
        ImageGenerationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        // Ensure model is initialized
        var initResult = await InitializeAsync();
        if (initResult.IsFailure)
        {
            return Result<ImageGenerationResponse>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Convert config to JS-friendly format
            object? jsConfig = null;
            if (config != null)
            {
                jsConfig = new
                {
                    numberOfImages = config.NumberOfImages,
                    aspectRatio = config.AspectRatio,
                    negativePrompt = config.NegativePrompt
                };
            }

            var result = await _jsInterop.AIGenerateImagesAsync(_modelName, prompt, jsConfig);

            if (result.Success && result.Data != null)
            {
                var images = result.Data.Images
                    .Select(img => new GeneratedImage
                    {
                        Base64Data = img.Base64Data,
                        MimeType = img.MimeType
                    })
                    .ToList();

                return Result<ImageGenerationResponse>.Success(
                    new ImageGenerationResponse { Images = images });
            }

            var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
            return Result<ImageGenerationResponse>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            return Result<ImageGenerationResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Result<ImageGenerationResponse>.Failure(
                new FirebaseError("ai/unknown", ex.Message));
        }
    }
}
