namespace FireBlazor;

/// <summary>
/// A message in a chat conversation.
/// </summary>
public sealed record ChatMessage
{
    /// <summary>
    /// The role of the message author.
    /// Valid values: "user" or "model".
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Content { get; init; }
}
