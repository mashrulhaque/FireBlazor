namespace FireBlazor;

/// <summary>
/// Configuration for AI content generation.
/// </summary>
public sealed record GenerationConfig
{
    /// <summary>
    /// Controls randomness in output. Lower values make output more deterministic.
    /// Valid range: 0.0 to 2.0.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Maximum number of tokens to generate in the response.
    /// </summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// Nucleus sampling parameter. Controls diversity via cumulative probability.
    /// Valid range: 0.0 to 1.0.
    /// </summary>
    public float? TopP { get; init; }

    /// <summary>
    /// Top-k sampling parameter. Limits token selection to k most probable tokens.
    /// </summary>
    public int? TopK { get; init; }

    /// <summary>
    /// Sequences that will stop generation when encountered.
    /// </summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>
    /// System instruction to guide the model's behavior.
    /// </summary>
    public string? SystemInstruction { get; init; }

    /// <summary>
    /// Safety settings to filter harmful content.
    /// Overrides the model's default safety settings.
    /// </summary>
    public IReadOnlyList<SafetySetting>? SafetySettings { get; init; }

    /// <summary>
    /// Tool definitions that the model can use.
    /// Currently supports function declarations.
    /// </summary>
    public IReadOnlyList<Tool>? Tools { get; init; }

    /// <summary>
    /// How the model should use tools.
    /// </summary>
    public ToolConfig? ToolConfig { get; init; }

    /// <summary>
    /// Grounding configuration for external data sources.
    /// </summary>
    public GroundingConfig? Grounding { get; init; }
}
