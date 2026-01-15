namespace FireBlazor;

/// <summary>
/// Options for starting a chat session.
/// </summary>
public sealed record ChatOptions
{
    /// <summary>
    /// Pre-existing conversation history.
    /// Use this to resume a previous conversation or provide context.
    /// </summary>
    public IReadOnlyList<ChatMessage>? History { get; init; }

    /// <summary>
    /// Generation config for all messages in this chat session.
    /// Overrides the model's default configuration.
    /// </summary>
    public GenerationConfig? GenerationConfig { get; init; }
}
