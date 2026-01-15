namespace FireBlazor;

/// <summary>
/// Configuration for grounding responses with external data sources.
/// </summary>
public sealed record GroundingConfig
{
    /// <summary>
    /// Enable grounding with Google Search.
    /// </summary>
    public bool GoogleSearchGrounding { get; init; }

    /// <summary>
    /// Dynamic retrieval configuration for Google Search grounding.
    /// </summary>
    public DynamicRetrievalConfig? DynamicRetrievalConfig { get; init; }

    /// <summary>
    /// Creates a config that enables Google Search grounding.
    /// </summary>
    public static GroundingConfig WithGoogleSearch() =>
        new() { GoogleSearchGrounding = true };

    /// <summary>
    /// Creates a config that enables Google Search grounding with dynamic threshold.
    /// </summary>
    public static GroundingConfig WithGoogleSearch(float dynamicThreshold) =>
        new()
        {
            GoogleSearchGrounding = true,
            DynamicRetrievalConfig = new DynamicRetrievalConfig
            {
                Mode = DynamicRetrievalMode.Dynamic,
                DynamicThreshold = dynamicThreshold
            }
        };
}

/// <summary>
/// Configuration for dynamic retrieval behavior.
/// </summary>
public sealed record DynamicRetrievalConfig
{
    /// <summary>
    /// The mode for dynamic retrieval.
    /// </summary>
    public DynamicRetrievalMode Mode { get; init; } = DynamicRetrievalMode.Unspecified;

    /// <summary>
    /// Threshold for dynamic retrieval (0.0 to 1.0).
    /// Lower values retrieve more often.
    /// </summary>
    public float? DynamicThreshold { get; init; }
}

/// <summary>
/// Mode for dynamic retrieval in grounding.
/// </summary>
public enum DynamicRetrievalMode
{
    /// <summary>Unspecified mode.</summary>
    Unspecified = 0,

    /// <summary>Always retrieve from grounding sources.</summary>
    Always = 1,

    /// <summary>Dynamically decide whether to retrieve based on content.</summary>
    Dynamic = 2
}
