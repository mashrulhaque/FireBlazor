namespace FireBlazor.Tests.AILogic;

public class IGenerativeModelTests
{
    [Fact]
    public void Interface_HasModelNameProperty()
    {
        // Compile-time verification that interface has expected members
        var type = typeof(IGenerativeModel);

        var property = type.GetProperty(nameof(IGenerativeModel.ModelName));
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void Interface_HasGenerateContentAsyncMethod()
    {
        var type = typeof(IGenerativeModel);

        var method = type.GetMethod(nameof(IGenerativeModel.GenerateContentAsync), new[] { typeof(string), typeof(CancellationToken) });
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Result<GenerateContentResponse>>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("prompt", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Interface_HasGenerateContentStreamAsyncMethod()
    {
        var type = typeof(IGenerativeModel);

        var method = type.GetMethod(nameof(IGenerativeModel.GenerateContentStreamAsync), new[] { typeof(string), typeof(CancellationToken) });
        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<Result<GenerateContentChunk>>), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("prompt", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Interface_HasStartChatMethod()
    {
        var method = typeof(IGenerativeModel).GetMethod("StartChat",
            new[] { typeof(ChatOptions) });

        Assert.NotNull(method);
        Assert.Equal(typeof(IChatSession), method!.ReturnType);
    }

    [Fact]
    public void Interface_StartChatHasOptionalParameter()
    {
        var method = typeof(IGenerativeModel).GetMethod("StartChat",
            new[] { typeof(ChatOptions) });
        var parameter = method?.GetParameters().FirstOrDefault();

        Assert.NotNull(parameter);
        Assert.True(parameter!.HasDefaultValue);
        Assert.Null(parameter.DefaultValue);
    }

    [Fact]
    public void MockImplementation_CanBeCreated()
    {
        // Verify interface can be implemented
        var mock = new MockGenerativeModel("test-model");

        Assert.Equal("test-model", mock.ModelName);
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
            var response = new GenerateContentResponse { Text = $"Response to: {prompt}" };
            return Task.FromResult(Result<GenerateContentResponse>.Success(response));
        }

        public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
            string prompt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new GenerateContentChunk { Text = "Hello ", IsFinal = false };
            yield return new GenerateContentChunk { Text = "World", IsFinal = true };
            await Task.CompletedTask;
        }

        public Task<Result<GenerateContentResponse>> GenerateContentAsync(
            IEnumerable<ContentPart> parts,
            CancellationToken cancellationToken = default)
        {
            var response = new GenerateContentResponse { Text = "Response to multimodal content" };
            return Task.FromResult(Result<GenerateContentResponse>.Success(response));
        }

        public async IAsyncEnumerable<Result<GenerateContentChunk>> GenerateContentStreamAsync(
            IEnumerable<ContentPart> parts,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new GenerateContentChunk { Text = "Multimodal ", IsFinal = false };
            yield return new GenerateContentChunk { Text = "Response", IsFinal = true };
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
