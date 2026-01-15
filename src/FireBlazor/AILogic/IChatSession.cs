namespace FireBlazor;

/// <summary>
/// Represents a multi-turn chat conversation with a generative AI model.
/// </summary>
/// <remarks>
/// A chat session maintains conversation history automatically.
/// Each message sent is added to the history, enabling contextual responses.
/// Dispose of the session when done to release resources.
/// </remarks>
public interface IChatSession : IAsyncDisposable
{
    /// <summary>
    /// The conversation history for this chat session.
    /// Includes both user messages and model responses.
    /// </summary>
    IReadOnlyList<ChatMessage> History { get; }

    /// <summary>
    /// Sends a message and returns the model's response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result containing the model's response or an error.</returns>
    Task<Result<GenerateContentResponse>> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message and streams the model's response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable of response chunks.</returns>
    IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multimodal content and receives a complete response.
    /// </summary>
    /// <param name="parts">The content parts to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<GenerateContentResponse>> SendMessageAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multimodal content and streams the response.
    /// </summary>
    /// <param name="parts">The content parts to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default);
}
