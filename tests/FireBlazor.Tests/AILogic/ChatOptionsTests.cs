namespace FireBlazor.Tests.AILogic;

public class ChatOptionsTests
{
    [Fact]
    public void ChatOptions_Default_HasNullHistory()
    {
        var options = new ChatOptions();

        Assert.Null(options.History);
    }

    [Fact]
    public void ChatOptions_Default_HasNullGenerationConfig()
    {
        var options = new ChatOptions();

        Assert.Null(options.GenerationConfig);
    }

    [Fact]
    public void ChatOptions_WithHistory_SetsHistoryCorrectly()
    {
        var history = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Hello" },
            new() { Role = "model", Content = "Hi!" }
        };

        var options = new ChatOptions { History = history };

        Assert.Equal(2, options.History?.Count);
        Assert.Equal("user", options.History?[0].Role);
    }

    [Fact]
    public void ChatOptions_WithGenerationConfig_SetsConfigCorrectly()
    {
        var config = new GenerationConfig { Temperature = 0.5f };

        var options = new ChatOptions { GenerationConfig = config };

        Assert.NotNull(options.GenerationConfig);
        Assert.Equal(0.5f, options.GenerationConfig?.Temperature);
    }

    [Fact]
    public void ChatOptions_WithBothHistoryAndConfig_SetsAllProperties()
    {
        var history = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Hello" }
        };
        var config = new GenerationConfig { MaxOutputTokens = 100 };

        var options = new ChatOptions
        {
            History = history,
            GenerationConfig = config
        };

        Assert.Single(options.History!);
        Assert.Equal(100, options.GenerationConfig?.MaxOutputTokens);
    }
}
