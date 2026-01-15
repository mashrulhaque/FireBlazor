using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IChatSession using JavaScript interop.
/// </summary>
internal sealed class WasmChatSession : IChatSession
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _modelName;
    private readonly List<ChatMessage> _history;
    private int _sessionId;
    private bool _initialized;
    private bool _disposed;

    internal WasmChatSession(FirebaseJsInterop jsInterop, string modelName, IEnumerable<ChatMessage>? initialHistory = null)
    {
        _jsInterop = jsInterop;
        _modelName = modelName;
        _history = initialHistory?.ToList() ?? [];
    }

    private async Task<Result<Unit>> EnsureInitializedAsync()
    {
        if (_initialized)
            return Result<Unit>.Success(Unit.Value);

        // Serialize history for JS interop
        string? historyJson = null;
        if (_history.Count > 0)
        {
            historyJson = JsonSerializer.Serialize(_history);
        }

        var result = await _jsInterop.AIStartChatAsync(_modelName, historyJson);

        if (result.Success && result.Data != null)
        {
            _sessionId = result.Data.SessionId;
            _initialized = true;
            return Result<Unit>.Success(Unit.Value);
        }

        var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
        return Result<Unit>.Failure(new FirebaseError(error.Code, error.Message));
    }

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public async Task<Result<GenerateContentResponse>> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

        // Ensure chat session is initialized
        var initResult = await EnsureInitializedAsync();
        if (initResult.IsFailure)
        {
            return Result<GenerateContentResponse>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AISendChatMessageAsync(_sessionId, message);

            if (result.Success && result.Data != null)
            {
                var response = new GenerateContentResponse
                {
                    Text = result.Data.Text,
                    Usage = result.Data.Usage != null
                        ? new TokenUsage
                        {
                            PromptTokens = result.Data.Usage.PromptTokens,
                            CandidateTokens = result.Data.Usage.CandidateTokens,
                            TotalTokens = result.Data.Usage.TotalTokens
                        }
                        : null,
                    FinishReason = (FinishReason)result.Data.FinishReason
                };

                // Add user message and model response to history
                _history.Add(new ChatMessage { Role = "user", Content = message });
                _history.Add(new ChatMessage { Role = "model", Content = response.Text });

                return Result<GenerateContentResponse>.Success(response);
            }

            var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
            return Result<GenerateContentResponse>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/unknown", ex.Message));
        }
    }

    public async IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

        // Ensure chat session is initialized
        var initResult = await EnsureInitializedAsync();
        if (initResult.IsFailure)
        {
            yield return Result<GenerateContentChunk>.Failure(initResult.Error!);
            yield break;
        }

        var channel = Channel.CreateUnbounded<Result<GenerateContentChunk>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        var callback = new StreamCallback(channel.Writer);
        var callbackRef = DotNetObjectReference.Create(callback);
        var fullText = new System.Text.StringBuilder();

        try
        {
            // Start streaming (non-blocking)
            _ = _jsInterop.AISendChatMessageStreamAsync(
                _sessionId,
                message,
                callbackRef,
                nameof(StreamCallback.OnStreamChunk));

            // Read from channel until complete
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (chunk.IsSuccess && chunk.Value != null)
                {
                    if (!chunk.Value.IsFinal)
                    {
                        fullText.Append(chunk.Value.Text);
                    }
                }

                yield return chunk;

                // Check if this was a final chunk or error
                if (chunk.IsSuccess && chunk.Value?.IsFinal == true)
                    break;
                if (chunk.IsFailure)
                    break;
            }

            // Add messages to history after successful streaming
            _history.Add(new ChatMessage { Role = "user", Content = message });
            _history.Add(new ChatMessage { Role = "model", Content = fullText.ToString() });
        }
        finally
        {
            callbackRef.Dispose();
        }
    }

    public async Task<Result<GenerateContentResponse>> SendMessageAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parts, nameof(parts));

        var jsParts = parts.Select(SerializeContentPart).ToArray();

        if (jsParts.Length == 0)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/invalid-argument", "Content parts cannot be empty"));
        }

        // Ensure chat session is initialized
        var initResult = await EnsureInitializedAsync();
        if (initResult.IsFailure)
        {
            return Result<GenerateContentResponse>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AISendChatMessageWithPartsAsync(_sessionId, jsParts);

            if (result.Success && result.Data != null)
            {
                var response = new GenerateContentResponse
                {
                    Text = result.Data.Text,
                    Usage = result.Data.Usage != null
                        ? new TokenUsage
                        {
                            PromptTokens = result.Data.Usage.PromptTokens,
                            CandidateTokens = result.Data.Usage.CandidateTokens,
                            TotalTokens = result.Data.Usage.TotalTokens
                        }
                        : null,
                    FinishReason = (FinishReason)result.Data.FinishReason
                };

                // Add user message with multimodal content description and model response to history
                var userContent = GetTextFromParts(parts);
                _history.Add(new ChatMessage { Role = "user", Content = userContent });
                _history.Add(new ChatMessage { Role = "model", Content = response.Text });

                return Result<GenerateContentResponse>.Success(response);
            }

            var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
            return Result<GenerateContentResponse>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/unknown", ex.Message));
        }
    }

    public async IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        IEnumerable<ContentPart> parts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parts, nameof(parts));

        var partsList = parts.ToList();
        var jsParts = partsList.Select(SerializeContentPart).ToArray();

        if (jsParts.Length == 0)
        {
            yield return Result<GenerateContentChunk>.Failure(
                new FirebaseError("ai/invalid-argument", "Content parts cannot be empty"));
            yield break;
        }

        // Ensure chat session is initialized
        var initResult = await EnsureInitializedAsync();
        if (initResult.IsFailure)
        {
            yield return Result<GenerateContentChunk>.Failure(initResult.Error!);
            yield break;
        }

        var channel = Channel.CreateUnbounded<Result<GenerateContentChunk>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        var callback = new StreamCallback(channel.Writer);
        var callbackRef = DotNetObjectReference.Create(callback);
        var fullText = new System.Text.StringBuilder();

        try
        {
            // Start streaming (non-blocking)
            _ = _jsInterop.AISendChatMessageStreamWithPartsAsync(
                _sessionId,
                jsParts,
                callbackRef,
                nameof(StreamCallback.OnStreamChunk));

            // Read from channel until complete
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (chunk.IsSuccess && chunk.Value != null)
                {
                    if (!chunk.Value.IsFinal)
                    {
                        fullText.Append(chunk.Value.Text);
                    }
                }

                yield return chunk;

                // Check if this was a final chunk or error
                if (chunk.IsSuccess && chunk.Value?.IsFinal == true)
                    break;
                if (chunk.IsFailure)
                    break;
            }

            // Add messages to history after successful streaming
            var userContent = GetTextFromParts(partsList);
            _history.Add(new ChatMessage { Role = "user", Content = userContent });
            _history.Add(new ChatMessage { Role = "model", Content = fullText.ToString() });
        }
        finally
        {
            callbackRef.Dispose();
        }
    }

    private static object SerializeContentPart(ContentPart part) => part switch
    {
        TextPart text => new { type = "text", text = text.Text },
        ImagePart image => new { type = "image", base64Data = Convert.ToBase64String(image.Data), mimeType = image.MimeType },
        Base64ImagePart base64 => new { type = "base64Image", base64Data = base64.Base64Data, mimeType = base64.MimeType },
        FileUriPart file => new { type = "fileUri", uri = file.Uri, mimeType = file.MimeType },
        _ => throw new ArgumentException($"Unknown ContentPart type: {part.GetType().Name}")
    };

    private static string GetTextFromParts(IEnumerable<ContentPart> parts)
    {
        var textParts = parts.OfType<TextPart>().Select(p => p.Text).ToList();
        if (textParts.Count > 0)
        {
            return string.Join(" ", textParts);
        }
        return "[multimodal content]";
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Only dispose the JS session if it was initialized
        if (_initialized)
        {
            try
            {
                await _jsInterop.AIDisposeChatSessionAsync(_sessionId);
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Callback handler for streaming chat message responses.
    /// </summary>
    private sealed class StreamCallback
    {
        private readonly ChannelWriter<Result<GenerateContentChunk>> _writer;

        public StreamCallback(ChannelWriter<Result<GenerateContentChunk>> writer)
        {
            _writer = writer;
        }

        [JSInvokable]
        public async Task OnStreamChunk(JsResult<JsStreamChunk> result)
        {
            if (result.Success && result.Data != null)
            {
                var chunk = new GenerateContentChunk
                {
                    Text = result.Data.Text,
                    IsFinal = result.Data.IsFinal
                };
                await _writer.WriteAsync(Result<GenerateContentChunk>.Success(chunk));

                if (result.Data.IsFinal)
                {
                    _writer.Complete();
                }
            }
            else
            {
                var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
                await _writer.WriteAsync(
                    Result<GenerateContentChunk>.Failure(new FirebaseError(error.Code, error.Message)));
                _writer.Complete();
            }
        }
    }
}
