namespace FireBlazor.Tests.AILogic;

public class GenerateContentResponseTests
{
    [Fact]
    public void GenerateContentResponse_RequiresText()
    {
        var response = new GenerateContentResponse { Text = "Hello, world!" };

        Assert.Equal("Hello, world!", response.Text);
    }

    [Fact]
    public void GenerateContentResponse_OptionalPropertiesDefaultNull()
    {
        var response = new GenerateContentResponse { Text = "Test" };

        Assert.Null(response.Usage);
        Assert.Null(response.FinishReason);
    }

    [Fact]
    public void GenerateContentResponse_WithAllProperties()
    {
        var usage = new TokenUsage
        {
            PromptTokens = 10,
            CandidateTokens = 50,
            TotalTokens = 60
        };

        var response = new GenerateContentResponse
        {
            Text = "Generated content",
            Usage = usage,
            FinishReason = FinishReason.Stop
        };

        Assert.Equal("Generated content", response.Text);
        Assert.Equal(usage, response.Usage);
        Assert.Equal(FinishReason.Stop, response.FinishReason);
    }

    [Fact]
    public void GenerateContentChunk_RequiresText()
    {
        var chunk = new GenerateContentChunk { Text = "chunk text" };

        Assert.Equal("chunk text", chunk.Text);
    }

    [Fact]
    public void GenerateContentChunk_IsFinalDefaultsFalse()
    {
        var chunk = new GenerateContentChunk { Text = "test" };

        Assert.False(chunk.IsFinal);
    }

    [Fact]
    public void GenerateContentChunk_WithIsFinal()
    {
        var chunk = new GenerateContentChunk { Text = "final", IsFinal = true };

        Assert.Equal("final", chunk.Text);
        Assert.True(chunk.IsFinal);
    }

    [Fact]
    public void TokenUsage_DefaultsToZero()
    {
        var usage = new TokenUsage();

        Assert.Equal(0, usage.PromptTokens);
        Assert.Equal(0, usage.CandidateTokens);
        Assert.Equal(0, usage.TotalTokens);
    }

    [Fact]
    public void TokenUsage_SetsAllProperties()
    {
        var usage = new TokenUsage
        {
            PromptTokens = 100,
            CandidateTokens = 200,
            TotalTokens = 300
        };

        Assert.Equal(100, usage.PromptTokens);
        Assert.Equal(200, usage.CandidateTokens);
        Assert.Equal(300, usage.TotalTokens);
    }

    [Theory]
    [InlineData(FinishReason.Unknown, 0)]
    [InlineData(FinishReason.Stop, 1)]
    [InlineData(FinishReason.MaxTokens, 2)]
    [InlineData(FinishReason.Safety, 3)]
    [InlineData(FinishReason.Recitation, 4)]
    [InlineData(FinishReason.Other, 5)]
    public void FinishReason_HasCorrectValues(FinishReason reason, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)reason);
    }

    [Fact]
    public void FinishReason_AllValuesExist()
    {
        var values = Enum.GetValues<FinishReason>();

        Assert.Equal(6, values.Length);
        Assert.Contains(FinishReason.Unknown, values);
        Assert.Contains(FinishReason.Stop, values);
        Assert.Contains(FinishReason.MaxTokens, values);
        Assert.Contains(FinishReason.Safety, values);
        Assert.Contains(FinishReason.Recitation, values);
        Assert.Contains(FinishReason.Other, values);
    }

    [Fact]
    public void RecordEquality_GenerateContentResponse()
    {
        var response1 = new GenerateContentResponse { Text = "hello", FinishReason = FinishReason.Stop };
        var response2 = new GenerateContentResponse { Text = "hello", FinishReason = FinishReason.Stop };
        var response3 = new GenerateContentResponse { Text = "world", FinishReason = FinishReason.Stop };

        Assert.Equal(response1, response2);
        Assert.NotEqual(response1, response3);
    }

    [Fact]
    public void RecordEquality_TokenUsage()
    {
        var usage1 = new TokenUsage { PromptTokens = 10, CandidateTokens = 20, TotalTokens = 30 };
        var usage2 = new TokenUsage { PromptTokens = 10, CandidateTokens = 20, TotalTokens = 30 };
        var usage3 = new TokenUsage { PromptTokens = 10, CandidateTokens = 25, TotalTokens = 35 };

        Assert.Equal(usage1, usage2);
        Assert.NotEqual(usage1, usage3);
    }
}
