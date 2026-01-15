namespace FireBlazor.Testing;

/// <summary>
/// Fake implementation of IImageModel for testing purposes.
/// </summary>
public sealed class FakeImageModel : IImageModel
{
    private readonly Queue<Result<ImageGenerationResponse>> _responses = new();
    private readonly List<(string Prompt, ImageGenerationConfig? Config)> _calls = new();

    public FakeImageModel(string modelName = "fake-imagen-model")
    {
        ModelName = modelName;
    }

    public string ModelName { get; }

    /// <summary>
    /// Gets the list of calls made to GenerateImagesAsync.
    /// </summary>
    public IReadOnlyList<(string Prompt, ImageGenerationConfig? Config)> Calls => _calls;

    /// <summary>
    /// Queues a response to be returned by the next call to GenerateImagesAsync.
    /// </summary>
    public void SetupResponse(ImageGenerationResponse response)
    {
        _responses.Enqueue(Result<ImageGenerationResponse>.Success(response));
    }

    /// <summary>
    /// Queues an error to be returned by the next call to GenerateImagesAsync.
    /// </summary>
    public void SetupError(string code, string message)
    {
        _responses.Enqueue(Result<ImageGenerationResponse>.Failure(new FirebaseError(code, message)));
    }

    public Task<Result<ImageGenerationResponse>> GenerateImagesAsync(
        string prompt,
        ImageGenerationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        _calls.Add((prompt, config));

        if (_responses.Count > 0)
        {
            return Task.FromResult(_responses.Dequeue());
        }

        // Default: return a single placeholder image
        var defaultResponse = new ImageGenerationResponse
        {
            Images =
            [
                new GeneratedImage
                {
                    Base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                    MimeType = "image/png"
                }
            ]
        };

        return Task.FromResult(Result<ImageGenerationResponse>.Success(defaultResponse));
    }
}
