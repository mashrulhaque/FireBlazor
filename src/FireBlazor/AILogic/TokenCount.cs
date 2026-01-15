namespace FireBlazor;

/// <summary>
/// Result of counting tokens for content.
/// </summary>
public sealed record TokenCount
{
    /// <summary>
    /// Total number of tokens in the content.
    /// </summary>
    public required int TotalTokens { get; init; }

    /// <summary>
    /// Number of tokens in text content.
    /// </summary>
    public int? TextTokens { get; init; }

    /// <summary>
    /// Number of tokens in image content.
    /// </summary>
    public int? ImageTokens { get; init; }

    /// <summary>
    /// Number of tokens in audio content.
    /// </summary>
    public int? AudioTokens { get; init; }

    /// <summary>
    /// Number of tokens in video content.
    /// </summary>
    public int? VideoTokens { get; init; }
}
