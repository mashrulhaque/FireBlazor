namespace FireBlazor.Tests.AILogic;

public class ChatSessionInterfaceTests
{
    [Fact]
    public void IChatSession_HasMultimodalSendMessageAsync()
    {
        var type = typeof(IChatSession);
        var method = type.GetMethod("SendMessageAsync", new[] { typeof(IEnumerable<ContentPart>), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Result<GenerateContentResponse>>), method.ReturnType);
    }

    [Fact]
    public void IChatSession_HasMultimodalSendMessageStreamAsync()
    {
        var type = typeof(IChatSession);
        var method = type.GetMethod("SendMessageStreamAsync", new[] { typeof(IEnumerable<ContentPart>), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<Result<GenerateContentChunk>>), method.ReturnType);
    }
}
