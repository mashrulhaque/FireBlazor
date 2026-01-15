namespace FireBlazor;

/// <summary>
/// Safety rating for generated content indicating probability of harm.
/// </summary>
public sealed record SafetyRating
{
    /// <summary>The category of potentially harmful content.</summary>
    public required HarmCategory Category { get; init; }

    /// <summary>The probability level of harmful content.</summary>
    public required HarmProbability Probability { get; init; }

    /// <summary>Whether the content was blocked due to this rating.</summary>
    public bool Blocked { get; init; }

    /// <summary>
    /// Checks if the probability is at or above the specified level.
    /// </summary>
    public bool IsProbabilityAtLeast(HarmProbability level) =>
        Probability >= level;
}
