namespace FireBlazor;

/// <summary>
/// Probability levels for content being harmful.
/// </summary>
public enum HarmProbability
{
    /// <summary>Unspecified probability.</summary>
    Unspecified = 0,

    /// <summary>Negligible probability of harm.</summary>
    Negligible = 1,

    /// <summary>Low probability of harm.</summary>
    Low = 2,

    /// <summary>Medium probability of harm.</summary>
    Medium = 3,

    /// <summary>High probability of harm.</summary>
    High = 4
}
