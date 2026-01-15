using System.Runtime.CompilerServices;

namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IFirebaseAI for testing.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FakeFirebaseAI : IFirebaseAI
{
    private readonly Dictionary<string, FakeGenerativeModel> _models = new();
    private string? _defaultResponse = "This is a fake AI response.";
    private FirebaseError? _simulatedError;
    private readonly List<string> _streamChunks = [];

    /// <summary>
    /// Configures the default response for generated content.
    /// </summary>
    public void ConfigureDefaultResponse(string response)
    {
        _defaultResponse = response;
    }

    /// <summary>
    /// Configures streaming response chunks.
    /// </summary>
    public void ConfigureStreamChunks(params string[] chunks)
    {
        _streamChunks.Clear();
        _streamChunks.AddRange(chunks);
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Simulates an error using AILogicErrorCode.
    /// </summary>
    public void SimulateError(AILogicErrorCode code, string message)
    {
        _simulatedError = new FirebaseError(code.ToFirebaseCode(), message);
    }

    /// <summary>
    /// Resets all state to initial values.
    /// </summary>
    public void Reset()
    {
        _models.Clear();
        _defaultResponse = "This is a fake AI response.";
        _simulatedError = null;
        _streamChunks.Clear();
    }

    public IGenerativeModel GetModel(string modelName, GenerationConfig? config = null)
    {
        var cacheKey = config != null
            ? $"{modelName}_{config.GetHashCode()}"
            : modelName;

        if (!_models.TryGetValue(cacheKey, out var model))
        {
            model = new FakeGenerativeModel(this, modelName, config);
            _models[cacheKey] = model;
        }

        return model;
    }

    public IImageModel GetImageModel(string modelName = "imagen-4.0-generate-001")
    {
        return new FakeImageModel(modelName);
    }

    internal FirebaseError? ConsumeSimulatedError()
    {
        var error = _simulatedError;
        _simulatedError = null;
        return error;
    }

    internal string? DefaultResponse => _defaultResponse;
    internal IReadOnlyList<string> StreamChunks => _streamChunks;
}

/// <summary>
/// Fake implementation of IGenerativeModel for testing.
/// </summary>
public sealed class FakeGenerativeModel : IGenerativeModel
{
    private readonly FakeFirebaseAI _parent;
    private readonly GenerationConfig? _config;

    public string ModelName { get; }

    internal FakeGenerativeModel(FakeFirebaseAI parent, string modelName, GenerationConfig? config)
    {
        _parent = parent;
        ModelName = modelName;
        _config = config;
    }

    public Task<Result<GenerateContentResponse>> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled")));
        }

        var error = _parent.ConsumeSimulatedError();
        if (error != null)
        {
            return Task.FromResult(Result<GenerateContentResponse>.Failure(error));
        }

        var response = new GenerateContentResponse
        {
            Text = _parent.DefaultResponse ?? $"Response to: {prompt}",
            Usage = new TokenUsage
            {
                PromptTokens = prompt.Split(' ').Length,
                CandidateTokens = 10,
                TotalTokens = prompt.Split(' ').Length + 10
            },
            FinishReason = FinishReason.Stop
        };

        return Task.FromResult(Result<GenerateContentResponse>.Success(response));
    }

    public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var error = _parent.ConsumeSimulatedError();
        if (error != null)
        {
            yield return Result<GenerateContentChunk>.Failure(error);
            yield break;
        }

        var chunks = _parent.StreamChunks.Count > 0
            ? _parent.StreamChunks
            : new[] { "Hello", " ", "World" };

        foreach (var chunk in chunks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield return Result<GenerateContentChunk>.Failure(
                    new FirebaseError("ai/cancelled", "Operation was cancelled"));
                yield break;
            }

            yield return Result<GenerateContentChunk>.Success(
                new GenerateContentChunk { Text = chunk, IsFinal = false });

            await Task.Yield();
        }

        yield return Result<GenerateContentChunk>.Success(
            new GenerateContentChunk { Text = "", IsFinal = true });
    }

    public Task<Result<GenerateContentResponse>> GenerateContentAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        // Extract text from parts for simple fake implementation
        var textParts = parts.OfType<TextPart>().Select(p => p.Text);
        var prompt = string.Join(" ", textParts);
        return GenerateContentAsync(prompt.Length > 0 ? prompt : "multimodal content", cancellationToken);
    }

    public IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        // Extract text from parts for simple fake implementation
        var textParts = parts.OfType<TextPart>().Select(p => p.Text);
        var prompt = string.Join(" ", textParts);
        return GenerateContentStreamAsync(prompt.Length > 0 ? prompt : "multimodal content", cancellationToken);
    }

    public IChatSession StartChat(ChatOptions? options = null)
    {
        return new FakeChatSession(this, options?.History);
    }

    private int _tokensPerWord = 2; // Configurable for testing

    /// <summary>
    /// Sets the number of tokens to count per word for testing.
    /// </summary>
    public void SetTokensPerWord(int tokensPerWord)
    {
        _tokensPerWord = tokensPerWord;
    }

    public Task<Result<TokenCount>> CountTokensAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Result<TokenCount>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled")));
        }

        var error = _parent.ConsumeSimulatedError();
        if (error != null)
        {
            return Task.FromResult(Result<TokenCount>.Failure(error));
        }

        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var tokenCount = wordCount * _tokensPerWord;

        return Task.FromResult(Result<TokenCount>.Success(new TokenCount
        {
            TotalTokens = tokenCount,
            TextTokens = tokenCount
        }));
    }

    public Task<Result<TokenCount>> CountTokensAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Result<TokenCount>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled")));
        }

        var error = _parent.ConsumeSimulatedError();
        if (error != null)
        {
            return Task.FromResult(Result<TokenCount>.Failure(error));
        }

        var totalTokens = 0;
        var textTokens = 0;
        var imageTokens = 0;

        foreach (var part in parts)
        {
            switch (part)
            {
                case TextPart textPart:
                    var words = textPart.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    textTokens += words * _tokensPerWord;
                    break;
                case ImagePart:
                case Base64ImagePart:
                    imageTokens += 258; // Fixed cost per image (typical)
                    break;
            }
        }

        totalTokens = textTokens + imageTokens;

        return Task.FromResult(Result<TokenCount>.Success(new TokenCount
        {
            TotalTokens = totalTokens,
            TextTokens = textTokens > 0 ? textTokens : null,
            ImageTokens = imageTokens > 0 ? imageTokens : null
        }));
    }
}
