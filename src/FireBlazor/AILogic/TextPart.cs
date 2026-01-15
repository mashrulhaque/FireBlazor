namespace FireBlazor;

/// <summary>
/// Text content for AI generation.
/// </summary>
public sealed record TextPart : ContentPart
{
    /// <summary>
    /// The text content.
    /// </summary>
    public new string Text { get; }

    /// <summary>
    /// Creates a new TextPart with the specified text.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextPart(string text) => Text = text;
}
