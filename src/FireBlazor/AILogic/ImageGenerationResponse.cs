namespace FireBlazor;

/// <summary>
/// Response from an image generation request containing generated images.
/// </summary>
public sealed record ImageGenerationResponse
{
    /// <summary>
    /// The generated images.
    /// </summary>
    public required IReadOnlyList<GeneratedImage> Images { get; init; }

    /// <summary>
    /// Convenience property to get the first image, or null if none generated.
    /// </summary>
    public GeneratedImage? FirstImage => Images.Count > 0 ? Images[0] : null;
}
