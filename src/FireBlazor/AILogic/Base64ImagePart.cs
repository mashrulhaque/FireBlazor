namespace FireBlazor;

/// <summary>
/// Image content from base64-encoded string for AI generation.
/// </summary>
/// <param name="Base64Data">The base64-encoded image data (without data URL prefix).</param>
/// <param name="MimeType">The MIME type (e.g., "image/png", "image/jpeg").</param>
public sealed record Base64ImagePart(string Base64Data, string MimeType) : ContentPart;
