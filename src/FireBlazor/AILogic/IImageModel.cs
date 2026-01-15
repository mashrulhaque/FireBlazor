namespace FireBlazor;

/// <summary>
/// Interface for image generation models (Imagen).
/// </summary>
public interface IImageModel
{
    /// <summary>
    /// The model name (e.g., "imagen-3.0-generate-002").
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates images from a text prompt.
    /// </summary>
    /// <param name="prompt">Text description of the desired image.</param>
    /// <param name="config">Optional configuration for image generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the generated images or an error.</returns>
    Task<Result<ImageGenerationResponse>> GenerateImagesAsync(
        string prompt,
        ImageGenerationConfig? config = null,
        CancellationToken cancellationToken = default);
}
