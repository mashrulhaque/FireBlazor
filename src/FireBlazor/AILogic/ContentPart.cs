namespace FireBlazor;

/// <summary>
/// Base type for content parts in multimodal AI requests.
/// Use factory methods to create specific content types.
/// </summary>
public abstract record ContentPart
{
    /// <summary>
    /// Creates a text content part.
    /// </summary>
    /// <param name="text">The text content.</param>
    public static ContentPart Text(string text) => new TextPart(text);

    /// <summary>
    /// Creates an image content part from raw bytes.
    /// </summary>
    /// <param name="data">The raw image bytes.</param>
    /// <param name="mimeType">The MIME type (e.g., "image/png").</param>
    public static ContentPart Image(byte[] data, string mimeType) => new ImagePart(data, mimeType);

    /// <summary>
    /// Creates an image content part from base64-encoded data.
    /// </summary>
    /// <param name="base64Data">The base64-encoded image data.</param>
    /// <param name="mimeType">The MIME type (e.g., "image/jpeg").</param>
    public static ContentPart FromBase64(string base64Data, string mimeType) => new Base64ImagePart(base64Data, mimeType);

    /// <summary>
    /// Creates a file content part from a Cloud Storage URI.
    /// </summary>
    /// <param name="uri">The file URI (e.g., "gs://bucket/path/file.pdf").</param>
    /// <param name="mimeType">The MIME type.</param>
    public static ContentPart FileUri(string uri, string mimeType) => new FileUriPart(uri, mimeType);
}
