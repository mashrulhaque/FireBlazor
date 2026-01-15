namespace FireBlazor;

/// <summary>
/// Metadata about grounding sources used in a response.
/// </summary>
public sealed record GroundingMetadata
{
    /// <summary>
    /// Search queries that were used for grounding.
    /// </summary>
    public IReadOnlyList<string>? SearchQueries { get; init; }

    /// <summary>
    /// Chunks of grounded content from search results.
    /// </summary>
    public IReadOnlyList<GroundingChunk>? GroundingChunks { get; init; }

    /// <summary>
    /// Support information linking response to grounding sources.
    /// </summary>
    public IReadOnlyList<GroundingSupport>? GroundingSupports { get; init; }

    /// <summary>
    /// Search entry point for user to explore grounding.
    /// </summary>
    public SearchEntryPoint? SearchEntryPoint { get; init; }
}

/// <summary>
/// A chunk of content retrieved for grounding.
/// </summary>
public sealed record GroundingChunk
{
    /// <summary>
    /// Web source information if from a webpage.
    /// </summary>
    public WebSource? Web { get; init; }
}

/// <summary>
/// Web source information.
/// </summary>
public sealed record WebSource
{
    /// <summary>The URI of the web page.</summary>
    public string? Uri { get; init; }

    /// <summary>The title of the web page.</summary>
    public string? Title { get; init; }
}

/// <summary>
/// Links response segments to grounding sources.
/// </summary>
public sealed record GroundingSupport
{
    /// <summary>
    /// Segment of the response that is grounded.
    /// </summary>
    public GroundingSegment? Segment { get; init; }

    /// <summary>
    /// Indices of grounding chunks that support this segment.
    /// </summary>
    public IReadOnlyList<int>? GroundingChunkIndices { get; init; }

    /// <summary>
    /// Confidence scores for each grounding chunk.
    /// </summary>
    public IReadOnlyList<float>? ConfidenceScores { get; init; }
}

/// <summary>
/// A segment of the response text.
/// </summary>
public sealed record GroundingSegment
{
    /// <summary>Start index of the segment in the response text.</summary>
    public int StartIndex { get; init; }

    /// <summary>End index of the segment in the response text.</summary>
    public int EndIndex { get; init; }

    /// <summary>The text of the segment.</summary>
    public string? Text { get; init; }
}

/// <summary>
/// Entry point for exploring grounding sources.
/// </summary>
public sealed record SearchEntryPoint
{
    /// <summary>Rendered HTML for displaying search entry point.</summary>
    public string? RenderedContent { get; init; }

    /// <summary>SDK blob for programmatic access.</summary>
    public string? SdkBlob { get; init; }
}
