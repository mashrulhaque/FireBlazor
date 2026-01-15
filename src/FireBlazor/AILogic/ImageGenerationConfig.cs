namespace FireBlazor;

/// <summary>
/// Configuration options for image generation with Imagen models.
/// </summary>
public sealed record ImageGenerationConfig
{
    /// <summary>
    /// Number of images to generate (1-4). Default: 1
    /// </summary>
    public int? NumberOfImages { get; init; }

    /// <summary>
    /// Aspect ratio of generated images.
    /// Supported values: "1:1", "16:9", "9:16", "4:3", "3:4"
    /// Default: "1:1"
    /// </summary>
    public string? AspectRatio { get; init; }

    /// <summary>
    /// Negative prompt describing what to avoid in the generated image.
    /// Example: "blurry, low quality, distorted"
    /// </summary>
    public string? NegativePrompt { get; init; }
}
