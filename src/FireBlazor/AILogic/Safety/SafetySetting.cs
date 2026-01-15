namespace FireBlazor;

/// <summary>
/// Configuration for filtering a specific category of harmful content.
/// </summary>
public sealed record SafetySetting
{
    /// <summary>The category of harmful content to filter.</summary>
    public required HarmCategory Category { get; init; }

    /// <summary>The threshold at which to block content.</summary>
    public required HarmBlockThreshold Threshold { get; init; }

    /// <summary>
    /// Creates a safety setting that blocks the specified category at medium and above.
    /// </summary>
    public static SafetySetting BlockMedium(HarmCategory category) =>
        new() { Category = category, Threshold = HarmBlockThreshold.BlockMediumAndAbove };

    /// <summary>
    /// Creates a safety setting that blocks the specified category at high only.
    /// </summary>
    public static SafetySetting BlockHigh(HarmCategory category) =>
        new() { Category = category, Threshold = HarmBlockThreshold.BlockOnlyHigh };

    /// <summary>
    /// Creates a safety setting that does not block the specified category.
    /// </summary>
    public static SafetySetting NoBlock(HarmCategory category) =>
        new() { Category = category, Threshold = HarmBlockThreshold.BlockNone };
}
