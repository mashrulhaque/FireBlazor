namespace FireBlazor;

/// <summary>
/// Threshold levels for blocking harmful content.
/// </summary>
public enum HarmBlockThreshold
{
    /// <summary>Unspecified threshold.</summary>
    Unspecified = 0,

    /// <summary>Block content with low probability of harm and above.</summary>
    BlockLowAndAbove = 1,

    /// <summary>Block content with medium probability of harm and above.</summary>
    BlockMediumAndAbove = 2,

    /// <summary>Block only content with high probability of harm.</summary>
    BlockOnlyHigh = 3,

    /// <summary>Do not block any content (use with caution).</summary>
    BlockNone = 4,

    /// <summary>Turn off the safety filter for this category.</summary>
    Off = 5
}
