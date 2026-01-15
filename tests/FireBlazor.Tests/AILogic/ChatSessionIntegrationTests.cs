using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

public class ChatSessionIntegrationTests
{
    private readonly FakeFirebaseAI _fakeAI;
    private readonly IGenerativeModel _model;

    public ChatSessionIntegrationTests()
    {
        _fakeAI = new FakeFirebaseAI();
        _model = _fakeAI.GetModel("test-model");
    }

    [Fact]
    public void StartChat_ReturnsIChatSession()
    {
        var chat = _model.StartChat();

        Assert.NotNull(chat);
        Assert.IsAssignableFrom<IChatSession>(chat);
    }

    [Fact]
    public void StartChat_WithOptions_SetsInitialHistory()
    {
        var history = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Hello" },
            new() { Role = "model", Content = "Hi there!" }
        };
        var options = new ChatOptions { History = history };

        var chat = _model.StartChat(options);

        Assert.Equal(2, chat.History.Count);
        Assert.Equal("Hello", chat.History[0].Content);
    }

    [Fact]
    public void StartChat_WithoutOptions_HasEmptyHistory()
    {
        var chat = _model.StartChat();

        Assert.Empty(chat.History);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsResponse()
    {
        _fakeAI.ConfigureDefaultResponse("Hello back!");
        var chat = _model.StartChat();

        var result = await chat.SendMessageAsync("Hello");

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello back!", result.Value?.Text);
    }

    [Fact]
    public async Task SendMessageAsync_AddsToHistory()
    {
        _fakeAI.ConfigureDefaultResponse("I'm fine, thanks!");
        var chat = _model.StartChat();

        await chat.SendMessageAsync("How are you?");

        Assert.Equal(2, chat.History.Count);
        Assert.Equal("user", chat.History[0].Role);
        Assert.Equal("How are you?", chat.History[0].Content);
        Assert.Equal("model", chat.History[1].Role);
        Assert.Equal("I'm fine, thanks!", chat.History[1].Content);
    }

    [Fact]
    public async Task SendMessageAsync_OnError_DoesNotAddToHistory()
    {
        _fakeAI.SimulateError(AILogicErrorCode.QuotaExceeded, "Rate limit hit");
        var chat = _model.StartChat();

        var result = await chat.SendMessageAsync("Hello");

        Assert.False(result.IsSuccess);
        Assert.Empty(chat.History);
    }

    [Fact]
    public async Task SendMessageAsync_MultipleTurns_AccumulatesHistory()
    {
        _fakeAI.ConfigureDefaultResponse("Response");
        var chat = _model.StartChat();

        await chat.SendMessageAsync("Message 1");
        await chat.SendMessageAsync("Message 2");
        await chat.SendMessageAsync("Message 3");

        Assert.Equal(6, chat.History.Count); // 3 user + 3 model
    }

    [Fact]
    public async Task SendMessageStreamAsync_YieldsChunks()
    {
        _fakeAI.ConfigureStreamChunks("Hello", " ", "World");
        var chat = _model.StartChat();

        var chunks = new List<string>();
        await foreach (var chunk in chat.SendMessageStreamAsync("Test"))
        {
            if (chunk.IsSuccess && !chunk.Value!.IsFinal)
            {
                chunks.Add(chunk.Value.Text ?? "");
            }
        }

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Hello", chunks[0]);
        Assert.Equal(" ", chunks[1]);
        Assert.Equal("World", chunks[2]);
    }

    [Fact]
    public async Task SendMessageStreamAsync_AddsToHistory()
    {
        _fakeAI.ConfigureStreamChunks("Hello", " ", "World");
        var chat = _model.StartChat();

        await foreach (var _ in chat.SendMessageStreamAsync("Test")) { }

        Assert.Equal(2, chat.History.Count);
        Assert.Equal("user", chat.History[0].Role);
        Assert.Equal("Test", chat.History[0].Content);
        Assert.Equal("model", chat.History[1].Role);
        Assert.Equal("Hello World", chat.History[1].Content);
    }

    [Fact]
    public async Task DisposeAsync_PreventsSubsequentCalls()
    {
        var chat = _model.StartChat();

        await chat.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => chat.SendMessageAsync("Test"));
    }
}
