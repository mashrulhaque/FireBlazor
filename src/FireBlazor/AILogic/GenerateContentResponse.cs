namespace FireBlazor;

/// <summary>
/// Response from a content generation request.
/// </summary>
public sealed record GenerateContentResponse
{
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Token usage statistics for the request.
    /// </summary>
    public TokenUsage? Usage { get; init; }

    /// <summary>
    /// The reason generation stopped.
    /// </summary>
    public FinishReason? FinishReason { get; init; }

    /// <summary>
    /// Safety ratings for the generated content.
    /// </summary>
    public IReadOnlyList<SafetyRating>? SafetyRatings { get; init; }

    /// <summary>
    /// Function calls requested by the model, if any.
    /// When present, you should execute these functions and provide responses.
    /// </summary>
    public IReadOnlyList<FunctionCall>? FunctionCalls { get; init; }

    /// <summary>
    /// Whether the model is requesting function calls.
    /// </summary>
    public bool HasFunctionCalls => FunctionCalls is { Count: > 0 };

    /// <summary>
    /// Checks if the content was blocked by any safety filter.
    /// </summary>
    public bool WasBlocked => SafetyRatings?.Any(r => r.Blocked) ?? false;

    /// <summary>
    /// Gets safety ratings that caused blocking, if any.
    /// </summary>
    public IEnumerable<SafetyRating> BlockingRatings =>
        SafetyRatings?.Where(r => r.Blocked) ?? Enumerable.Empty<SafetyRating>();

    /// <summary>
    /// Grounding metadata if grounding was used for this response.
    /// </summary>
    public GroundingMetadata? GroundingMetadata { get; init; }

    /// <summary>
    /// Whether the response was grounded with external sources.
    /// </summary>
    public bool IsGrounded => GroundingMetadata?.GroundingChunks is { Count: > 0 };

    /// <summary>
    /// Gets the web sources used for grounding.
    /// </summary>
    public IEnumerable<WebSource> GroundingSources =>
        GroundingMetadata?.GroundingChunks?
            .Where(c => c.Web != null)
            .Select(c => c.Web!) ??
        Enumerable.Empty<WebSource>();
}

/// <summary>
/// A chunk of streamed content generation response.
/// </summary>
public sealed record GenerateContentChunk
{
    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Whether this is the final chunk in the stream.
    /// </summary>
    public bool IsFinal { get; init; }
}

/// <summary>
/// Token usage statistics for a generation request.
/// </summary>
public sealed record TokenUsage
{
    /// <summary>
    /// Number of tokens in the input prompt.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens in the generated response.
    /// </summary>
    public int CandidateTokens { get; init; }

    /// <summary>
    /// Total tokens used (prompt + candidate).
    /// </summary>
    public int TotalTokens { get; init; }
}

/// <summary>
/// Reason why content generation stopped.
/// </summary>
public enum FinishReason
{
    /// <summary>Unknown or unspecified finish reason.</summary>
    Unknown = 0,

    /// <summary>Natural stop, model finished generating.</summary>
    Stop = 1,

    /// <summary>Reached maximum output token limit.</summary>
    MaxTokens = 2,

    /// <summary>Content blocked due to safety filters.</summary>
    Safety = 3,

    /// <summary>Content blocked due to recitation/copyright concerns.</summary>
    Recitation = 4,

    /// <summary>Other unspecified reason.</summary>
    Other = 5
}
