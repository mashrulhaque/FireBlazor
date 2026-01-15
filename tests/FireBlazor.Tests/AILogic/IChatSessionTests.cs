namespace FireBlazor.Tests.AILogic;

public class IChatSessionTests
{
    [Fact]
    public void Interface_InheritsFromIAsyncDisposable()
    {
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IChatSession)));
    }

    [Fact]
    public void Interface_HasHistoryProperty()
    {
        var property = typeof(IChatSession).GetProperty("History");

        Assert.NotNull(property);
        Assert.Equal(typeof(IReadOnlyList<ChatMessage>), property.PropertyType);
    }

    [Fact]
    public void Interface_HasSendMessageAsyncMethod()
    {
        var method = typeof(IChatSession).GetMethod("SendMessageAsync",
            new[] { typeof(string), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Result<GenerateContentResponse>>), method.ReturnType);
    }

    [Fact]
    public void Interface_HasSendMessageStreamAsyncMethod()
    {
        var method = typeof(IChatSession).GetMethod("SendMessageStreamAsync",
            new[] { typeof(string), typeof(CancellationToken) });

        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<Result<GenerateContentChunk>>), method.ReturnType);
    }
}
