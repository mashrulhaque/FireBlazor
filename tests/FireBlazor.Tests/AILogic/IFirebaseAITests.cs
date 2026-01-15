namespace FireBlazor.Tests.AILogic;

public class IFirebaseAITests
{
    [Fact]
    public void Interface_HasGetModelMethod()
    {
        // Compile-time verification that interface has expected members
        var type = typeof(IFirebaseAI);

        var method = type.GetMethod(nameof(IFirebaseAI.GetModel));
        Assert.NotNull(method);
        Assert.Equal(typeof(IGenerativeModel), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("modelName", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("config", parameters[1].Name);
        Assert.Equal(typeof(GenerationConfig), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.Null(parameters[1].DefaultValue);
    }

    [Fact]
    public void MockImplementation_CanBeCreated()
    {
        // Verify interface can be implemented
        var mock = new MockFirebaseAI();

        var model = mock.GetModel("gemini-2.0-flash");
        Assert.NotNull(model);
        Assert.Equal("gemini-2.0-flash", model.ModelName);
    }

    [Fact]
    public void MockImplementation_AcceptsConfig()
    {
        var mock = new MockFirebaseAI();
        var config = new GenerationConfig { Temperature = 0.5f };

        var model = mock.GetModel("gemini-2.0-flash", config);
        Assert.NotNull(model);
    }

    [Fact]
    public void MockImplementation_AcceptsNullConfig()
    {
        var mock = new MockFirebaseAI();

        var model = mock.GetModel("gemini-2.0-flash", null);
        Assert.NotNull(model);
    }

    private sealed class MockFirebaseAI : IFirebaseAI
    {
        public IGenerativeModel GetModel(string modelName, GenerationConfig? config = null)
        {
            return new MockGenerativeModel(modelName);
        }

        public IImageModel GetImageModel(string modelName = "imagen-4.0-generate-001")
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MockGenerativeModel : IGenerativeModel
    {
        public string ModelName { get; }

        public MockGenerativeModel(string modelName)
        {
            ModelName = modelName;
        }

        public Task<Result<GenerateContentResponse>> GenerateContentAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var response = new GenerateContentResponse { Text = "mock response" };
            return Task.FromResult(Result<GenerateContentResponse>.Success(response));
        }

        public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
            string prompt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new GenerateContentChunk { Text = "mock", IsFinal = true };
            await Task.CompletedTask;
        }

        public Task<Result<GenerateContentResponse>> GenerateContentAsync(
            IEnumerable<ContentPart> parts,
            CancellationToken cancellationToken = default)
        {
            var response = new GenerateContentResponse { Text = "mock multimodal response" };
            return Task.FromResult(Result<GenerateContentResponse>.Success(response));
        }

        public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
            IEnumerable<ContentPart> parts,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new GenerateContentChunk { Text = "mock multimodal", IsFinal = true };
            await Task.CompletedTask;
        }

        public IChatSession StartChat(ChatOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TokenCount>> CountTokensAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TokenCount>> CountTokensAsync(
            IEnumerable<ContentPart> parts,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
