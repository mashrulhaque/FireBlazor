namespace FireBlazor;

/// <summary>
/// Categories of potentially harmful content that can be filtered.
/// </summary>
public enum HarmCategory
{
    /// <summary>Unspecified harm category.</summary>
    Unspecified = 0,

    /// <summary>Hate speech and content promoting discrimination.</summary>
    HateSpeech = 1,

    /// <summary>Content promoting dangerous activities or violence.</summary>
    DangerousContent = 2,

    /// <summary>Harassment and bullying content.</summary>
    Harassment = 3,

    /// <summary>Sexually explicit content.</summary>
    SexuallyExplicit = 4,

    /// <summary>Civic integrity concerns (elections, voting, etc.).</summary>
    CivicIntegrity = 5
}
