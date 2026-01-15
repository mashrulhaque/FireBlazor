namespace FireBlazor;

/// <summary>
/// Image content from raw bytes for AI generation.
/// </summary>
/// <param name="Data">The raw image bytes.</param>
/// <param name="MimeType">The MIME type (e.g., "image/png", "image/jpeg").</param>
public sealed record ImagePart(byte[] Data, string MimeType) : ContentPart;
