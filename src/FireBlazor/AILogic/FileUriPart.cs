namespace FireBlazor;

/// <summary>
/// File content from a Cloud Storage URI for AI generation.
/// Supports gs:// URIs for Firebase/Google Cloud Storage files.
/// </summary>
/// <param name="Uri">The file URI (e.g., "gs://bucket/path/file.pdf").</param>
/// <param name="MimeType">The MIME type (e.g., "application/pdf", "image/png").</param>
public sealed record FileUriPart(string Uri, string MimeType) : ContentPart;
