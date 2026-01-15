namespace FireBlazor.Tests.AILogic;

public class GenerativeModelInterfaceTests
{
    [Fact]
    public void IGenerativeModel_HasMultimodalGenerateContentAsync()
    {
        var type = typeof(IGenerativeModel);
        var method = type.GetMethod("GenerateContentAsync", new[] { typeof(IEnumerable<ContentPart>), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Result<GenerateContentResponse>>), method.ReturnType);
    }

    [Fact]
    public void IGenerativeModel_HasMultimodalGenerateContentStreamAsync()
    {
        var type = typeof(IGenerativeModel);
        var method = type.GetMethod("GenerateContentStreamAsync", new[] { typeof(IEnumerable<ContentPart>), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<Result<GenerateContentChunk>>), method.ReturnType);
    }
}
