namespace FireBlazor;

/// <summary>
/// Represents a single generated image from Imagen.
/// </summary>
public sealed record GeneratedImage
{
    /// <summary>
    /// Base64-encoded image data.
    /// </summary>
    public required string Base64Data { get; init; }

    /// <summary>
    /// MIME type of the image (e.g., "image/png", "image/jpeg").
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Converts the image to a data URL for direct use in img src attributes.
    /// </summary>
    /// <returns>A data URL string (e.g., "data:image/png;base64,...")</returns>
    public string ToDataUrl() => $"data:{MimeType};base64,{Base64Data}";
}
