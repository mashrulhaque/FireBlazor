namespace FireBlazor;

/// <summary>
/// Interface for interacting with a generative AI model.
/// </summary>
public interface IGenerativeModel
{
    /// <summary>
    /// The name of the model being used.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates content based on the provided prompt.
    /// </summary>
    /// <param name="prompt">The text prompt to generate content from.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result containing the generated content or an error.</returns>
    Task<Result<GenerateContentResponse>> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content from multimodal input (text, images, files).
    /// </summary>
    /// <param name="parts">The content parts to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<GenerateContentResponse>> GenerateContentAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content as a stream of chunks.
    /// </summary>
    /// <param name="prompt">The text prompt to generate content from.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable of content chunks.</returns>
    IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams generated content from multimodal input.
    /// </summary>
    /// <param name="parts">The content parts to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a multi-turn chat session.
    /// </summary>
    /// <param name="options">Optional chat session configuration including history and generation config.</param>
    /// <returns>A chat session for multi-turn conversation.</returns>
    IChatSession StartChat(ChatOptions? options = null);

    /// <summary>
    /// Counts tokens for a text prompt without generating content.
    /// Useful for estimating costs and checking context limits.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token count result.</returns>
    Task<Result<TokenCount>> CountTokensAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts tokens for multimodal content without generating.
    /// </summary>
    /// <param name="parts">The content parts to count tokens for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token count result.</returns>
    Task<Result<TokenCount>> CountTokensAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default);
}
