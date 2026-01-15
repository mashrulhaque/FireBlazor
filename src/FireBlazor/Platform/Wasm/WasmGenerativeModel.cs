using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.JSInterop;

namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WASM implementation of IGenerativeModel using JavaScript interop.
/// </summary>
internal sealed class WasmGenerativeModel : IGenerativeModel
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly string _modelName;
    private readonly GenerationConfig? _config;
    private bool _initialized;

    public WasmGenerativeModel(FirebaseJsInterop jsInterop, string modelName, GenerationConfig? config)
    {
        _jsInterop = jsInterop;
        _modelName = modelName;
        _config = config;
    }

    public string ModelName => _modelName;

    internal async Task<Result<Unit>> InitializeAsync()
    {
        if (_initialized)
            return Result<Unit>.Success(Unit.Value);

        // Convert GenerationConfig to a JS-friendly anonymous object
        object? jsConfig = null;
        if (_config != null)
        {
            jsConfig = new
            {
                temperature = _config.Temperature,
                maxOutputTokens = _config.MaxOutputTokens,
                topP = _config.TopP,
                topK = _config.TopK,
                stopSequences = _config.StopSequences?.ToArray(),
                systemInstruction = _config.SystemInstruction,
                safetySettings = _config.SafetySettings?.Select(s => new
                {
                    category = (int)s.Category,
                    threshold = (int)s.Threshold
                }).ToArray(),
                tools = _config.Tools?.Select(t => new
                {
                    functionDeclarations = t.FunctionDeclarations?.Select(fd => new
                    {
                        name = fd.Name,
                        description = fd.Description,
                        parameters = fd.Parameters != null ? new
                        {
                            type = fd.Parameters.Type,
                            properties = fd.Parameters.Properties?.ToDictionary(
                                kvp => kvp.Key,
                                kvp => new
                                {
                                    type = kvp.Value.Type,
                                    description = kvp.Value.Description
                                }
                            ),
                            required = fd.Parameters.Required?.ToArray()
                        } : null
                    }).ToArray()
                }).ToArray(),
                toolConfig = _config.ToolConfig != null ? new
                {
                    mode = (int)_config.ToolConfig.Mode,
                    allowedFunctionNames = _config.ToolConfig.AllowedFunctionNames
                } : null,
                grounding = _config.Grounding != null ? new
                {
                    googleSearchGrounding = _config.Grounding.GoogleSearchGrounding,
                    dynamicRetrievalConfig = _config.Grounding.DynamicRetrievalConfig != null ? new
                    {
                        mode = (int)_config.Grounding.DynamicRetrievalConfig.Mode,
                        dynamicThreshold = _config.Grounding.DynamicRetrievalConfig.DynamicThreshold
                    } : null
                } : null
            };
        }

        var result = await _jsInterop.AIGetGenerativeModelAsync(_modelName, jsConfig);

        if (result.Success)
        {
            _initialized = true;
            return Result<Unit>.Success(Unit.Value);
        }

        var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Unknown error" };
        return Result<Unit>.Failure(new FirebaseError(error.Code, error.Message));
    }

    public async Task<Result<GenerateContentResponse>> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        // Ensure model is initialized
        var initResult = await InitializeAsync();
        if (initResult.IsFailure)
        {
            return Result<GenerateContentResponse>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AIGenerateContentAsync(_modelName, prompt);

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
                    FinishReason = (FinishReason)result.Data.FinishReason,
                    SafetyRatings = result.Data.SafetyRatings?.Select(r => new SafetyRating
                    {
                        Category = (HarmCategory)r.Category,
                        Probability = (HarmProbability)r.Probability,
                        Blocked = r.Blocked
                    }).ToList(),
                    FunctionCalls = result.Data.FunctionCalls?.Select(fc => new FunctionCall
                    {
                        Name = fc.Name,
                        Arguments = fc.Arguments
                    }).ToList(),
                    GroundingMetadata = result.Data.GroundingMetadata != null ? new GroundingMetadata
                    {
                        SearchQueries = result.Data.GroundingMetadata.SearchQueries,
                        GroundingChunks = result.Data.GroundingMetadata.GroundingChunks?
                            .Select(c => new GroundingChunk
                            {
                                Web = c.Web != null ? new WebSource { Uri = c.Web.Uri, Title = c.Web.Title } : null
                            }).ToList(),
                        GroundingSupports = result.Data.GroundingMetadata.GroundingSupports?
                            .Select(s => new GroundingSupport
                            {
                                Segment = s.Segment != null ? new GroundingSegment
                                {
                                    StartIndex = s.Segment.StartIndex,
                                    EndIndex = s.Segment.EndIndex,
                                    Text = s.Segment.Text
                                } : null,
                                GroundingChunkIndices = s.GroundingChunkIndices,
                                ConfidenceScores = s.ConfidenceScores
                            }).ToList(),
                        SearchEntryPoint = result.Data.GroundingMetadata.SearchEntryPoint != null ? new SearchEntryPoint
                        {
                            RenderedContent = result.Data.GroundingMetadata.SearchEntryPoint.RenderedContent,
                            SdkBlob = result.Data.GroundingMetadata.SearchEntryPoint.SdkBlob
                        } : null
                    } : null
                };

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

    public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        // Ensure model is initialized
        var initResult = await InitializeAsync();
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

        try
        {
            // Start streaming (non-blocking)
            _ = _jsInterop.AIGenerateContentStreamAsync(
                _modelName,
                prompt,
                callbackRef,
                nameof(StreamCallback.OnStreamChunk));

            // Read from channel until complete
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return chunk;

                // Check if this was a final chunk or error
                if (chunk.IsSuccess && chunk.Value.IsFinal)
                    break;
                if (chunk.IsFailure)
                    break;
            }
        }
        finally
        {
            callbackRef.Dispose();
        }
    }

    public async Task<Result<GenerateContentResponse>> GenerateContentAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parts, nameof(parts));

        var jsParts = parts.Select(SerializeContentPart).ToArray();

        if (jsParts.Length == 0)
        {
            return Result<GenerateContentResponse>.Failure(
                new FirebaseError("ai/invalid-argument", "Content parts cannot be empty"));
        }

        // Ensure model is initialized
        var initResult = await InitializeAsync();
        if (initResult.IsFailure)
        {
            return Result<GenerateContentResponse>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AIGenerateContentWithPartsAsync(_modelName, jsParts);

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
                    FinishReason = (FinishReason)result.Data.FinishReason,
                    SafetyRatings = result.Data.SafetyRatings?.Select(r => new SafetyRating
                    {
                        Category = (HarmCategory)r.Category,
                        Probability = (HarmProbability)r.Probability,
                        Blocked = r.Blocked
                    }).ToList(),
                    FunctionCalls = result.Data.FunctionCalls?.Select(fc => new FunctionCall
                    {
                        Name = fc.Name,
                        Arguments = fc.Arguments
                    }).ToList(),
                    GroundingMetadata = result.Data.GroundingMetadata != null ? new GroundingMetadata
                    {
                        SearchQueries = result.Data.GroundingMetadata.SearchQueries,
                        GroundingChunks = result.Data.GroundingMetadata.GroundingChunks?
                            .Select(c => new GroundingChunk
                            {
                                Web = c.Web != null ? new WebSource { Uri = c.Web.Uri, Title = c.Web.Title } : null
                            }).ToList(),
                        GroundingSupports = result.Data.GroundingMetadata.GroundingSupports?
                            .Select(s => new GroundingSupport
                            {
                                Segment = s.Segment != null ? new GroundingSegment
                                {
                                    StartIndex = s.Segment.StartIndex,
                                    EndIndex = s.Segment.EndIndex,
                                    Text = s.Segment.Text
                                } : null,
                                GroundingChunkIndices = s.GroundingChunkIndices,
                                ConfidenceScores = s.ConfidenceScores
                            }).ToList(),
                        SearchEntryPoint = result.Data.GroundingMetadata.SearchEntryPoint != null ? new SearchEntryPoint
                        {
                            RenderedContent = result.Data.GroundingMetadata.SearchEntryPoint.RenderedContent,
                            SdkBlob = result.Data.GroundingMetadata.SearchEntryPoint.SdkBlob
                        } : null
                    } : null
                };

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

    private static object SerializeContentPart(ContentPart part) => part switch
    {
        TextPart text => new { type = "text", text = text.Text },
        ImagePart image => new { type = "image", base64Data = Convert.ToBase64String(image.Data), mimeType = image.MimeType },
        Base64ImagePart base64 => new { type = "base64Image", base64Data = base64.Base64Data, mimeType = base64.MimeType },
        FileUriPart file => new { type = "fileUri", uri = file.Uri, mimeType = file.MimeType },
        _ => throw new ArgumentException($"Unknown ContentPart type: {part.GetType().Name}")
    };

    public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
        IEnumerable<ContentPart> parts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parts, nameof(parts));

        var jsParts = parts.Select(SerializeContentPart).ToArray();

        if (jsParts.Length == 0)
        {
            yield return Result<GenerateContentChunk>.Failure(
                new FirebaseError("ai/invalid-argument", "Content parts cannot be empty"));
            yield break;
        }

        // Ensure model is initialized
        var initResult = await InitializeAsync();
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

        try
        {
            // Start streaming (non-blocking)
            _ = _jsInterop.AIGenerateContentStreamWithPartsAsync(
                _modelName,
                jsParts,
                callbackRef,
                nameof(StreamCallback.OnStreamChunk));

            // Read from channel until complete
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return chunk;

                // Check if this was a final chunk or error
                if (chunk.IsSuccess && chunk.Value.IsFinal)
                    break;
                if (chunk.IsFailure)
                    break;
            }
        }
        finally
        {
            callbackRef.Dispose();
        }
    }

    public IChatSession StartChat(ChatOptions? options = null)
    {
        return new WasmChatSession(_jsInterop, _modelName, options?.History);
    }

    public async Task<Result<TokenCount>> CountTokensAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        // Ensure model is initialized
        var initResult = await InitializeAsync();
        if (initResult.IsFailure)
        {
            return Result<TokenCount>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AICountTokensAsync(_modelName, text);

            if (result.Success && result.Data != null)
            {
                return Result<TokenCount>.Success(new TokenCount
                {
                    TotalTokens = result.Data.TotalTokens,
                    TextTokens = result.Data.TextTokens,
                    ImageTokens = result.Data.ImageTokens,
                    AudioTokens = result.Data.AudioTokens,
                    VideoTokens = result.Data.VideoTokens
                });
            }

            var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Token counting failed" };
            return Result<TokenCount>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            return Result<TokenCount>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Result<TokenCount>.Failure(
                new FirebaseError("ai/unknown", ex.Message));
        }
    }

    public async Task<Result<TokenCount>> CountTokensAsync(
        IEnumerable<ContentPart> parts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parts, nameof(parts));

        var jsParts = parts.Select(SerializeContentPart).ToArray();

        if (jsParts.Length == 0)
        {
            return Result<TokenCount>.Failure(
                new FirebaseError("ai/invalid-argument", "Content parts cannot be empty"));
        }

        // Ensure model is initialized
        var initResult = await InitializeAsync();
        if (initResult.IsFailure)
        {
            return Result<TokenCount>.Failure(initResult.Error!);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _jsInterop.AICountTokensWithPartsAsync(_modelName, jsParts);

            if (result.Success && result.Data != null)
            {
                return Result<TokenCount>.Success(new TokenCount
                {
                    TotalTokens = result.Data.TotalTokens,
                    TextTokens = result.Data.TextTokens,
                    ImageTokens = result.Data.ImageTokens,
                    AudioTokens = result.Data.AudioTokens,
                    VideoTokens = result.Data.VideoTokens
                });
            }

            var error = result.Error ?? new JsError { Code = "ai/unknown", Message = "Token counting failed" };
            return Result<TokenCount>.Failure(new FirebaseError(error.Code, error.Message));
        }
        catch (OperationCanceledException)
        {
            return Result<TokenCount>.Failure(
                new FirebaseError("ai/cancelled", "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Result<TokenCount>.Failure(
                new FirebaseError("ai/unknown", ex.Message));
        }
    }

    /// <summary>
    /// Callback handler for streaming content generation.
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
