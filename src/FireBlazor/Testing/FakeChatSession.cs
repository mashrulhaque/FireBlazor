using System.Runtime.CompilerServices;

namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IChatSession for testing.
/// </summary>
public sealed class FakeChatSession : IChatSession
{
    private readonly FakeGenerativeModel _model;
    private readonly List<ChatMessage> _history;
    private bool _disposed;

    internal FakeChatSession(FakeGenerativeModel model, IEnumerable<ChatMessage>? initialHistory = null)
    {
        _model = model;
        _history = initialHistory?.ToList() ?? [];
    }

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public async Task<Result<GenerateContentResponse>> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }

        // Delegate to model's GenerateContentAsync (it handles error simulation)
        var result = await _model.GenerateContentAsync(message, cancellationToken);

        if (result.IsSuccess)
        {
            _history.Add(new ChatMessage { Role = "user", Content = message });
            _history.Add(new ChatMessage { Role = "model", Content = result.Value!.Text ?? "" });
        }

        return result;
    }

    public async IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var fullText = new System.Text.StringBuilder();
        var hasError = false;

        await foreach (var chunk in _model.GenerateContentStreamAsync(message, cancellationToken))
        {
            if (chunk.IsSuccess && chunk.Value != null && !chunk.Value.IsFinal)
            {
                fullText.Append(chunk.Value.Text);
            }
            else if (!chunk.IsSuccess)
            {
                hasError = true;
            }

            yield return chunk;

            if (chunk.IsSuccess && chunk.Value?.IsFinal == true)
            {
                break;
            }
        }

        if (!hasError)
        {
            _history.Add(new ChatMessage { Role = "user", Content = message });
            _history.Add(new ChatMessage { Role = "model", Content = fullText.ToString() });
        }
    }

    public async Task<Result<GenerateContentResponse>> SendMessageAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (cancellationToken.IsCancellationRequested)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }

        // Delegate to model's multimodal GenerateContentAsync
        var result = await _model.GenerateContentAsync(parts, cancellationToken);

        if (result.IsSuccess)
        {
            var userContent = GetTextFromParts(parts);
            _history.Add(new ChatMessage { Role = "user", Content = userContent });
            _history.Add(new ChatMessage { Role = "model", Content = result.Value!.Text ?? "" });
        }

        return result;
    }

    public async IAsyncEnumerable<Result<GenerateContentChunk>> SendMessageStreamAsync(
        IEnumerable<ContentPart> parts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var partsList = parts.ToList();
        var fullText = new System.Text.StringBuilder();
        var hasError = false;

        await foreach (var chunk in _model.GenerateContentStreamAsync(partsList, cancellationToken))
        {
            if (chunk.IsSuccess && chunk.Value != null && !chunk.Value.IsFinal)
            {
                fullText.Append(chunk.Value.Text);
            }
            else if (!chunk.IsSuccess)
            {
                hasError = true;
            }

            yield return chunk;

            if (chunk.IsSuccess && chunk.Value?.IsFinal == true)
            {
                break;
            }
        }

        if (!hasError)
        {
            var userContent = GetTextFromParts(partsList);
            _history.Add(new ChatMessage { Role = "user", Content = userContent });
            _history.Add(new ChatMessage { Role = "model", Content = fullText.ToString() });
        }
    }

    private static string GetTextFromParts(IEnumerable<ContentPart> parts)
    {
        var textParts = parts.OfType<TextPart>().Select(p => p.Text).ToList();
        if (textParts.Count > 0)
        {
            return string.Join(" ", textParts);
        }
        return "[multimodal content]";
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FakeChatSession));
        }
    }
}
