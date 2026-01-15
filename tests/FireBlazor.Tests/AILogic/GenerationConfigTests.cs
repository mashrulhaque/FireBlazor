namespace FireBlazor.Tests.AILogic;

public class GenerationConfigTests
{
    [Fact]
    public void DefaultValues_AreNull()
    {
        var config = new GenerationConfig();

        Assert.Null(config.Temperature);
        Assert.Null(config.MaxOutputTokens);
        Assert.Null(config.TopP);
        Assert.Null(config.TopK);
        Assert.Null(config.StopSequences);
        Assert.Null(config.SystemInstruction);
    }

    [Fact]
    public void InitProperties_SetValuesCorrectly()
    {
        var stopSequences = new[] { "STOP", "END" };
        var config = new GenerationConfig
        {
            Temperature = 0.7f,
            MaxOutputTokens = 1024,
            TopP = 0.9f,
            TopK = 40,
            StopSequences = stopSequences,
            SystemInstruction = "You are a helpful assistant."
        };

        Assert.Equal(0.7f, config.Temperature);
        Assert.Equal(1024, config.MaxOutputTokens);
        Assert.Equal(0.9f, config.TopP);
        Assert.Equal(40, config.TopK);
        Assert.Equal(stopSequences, config.StopSequences);
        Assert.Equal("You are a helpful assistant.", config.SystemInstruction);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        var config1 = new GenerationConfig
        {
            Temperature = 0.5f,
            MaxOutputTokens = 512
        };

        var config2 = new GenerationConfig
        {
            Temperature = 0.5f,
            MaxOutputTokens = 512
        };

        var config3 = new GenerationConfig
        {
            Temperature = 0.5f,
            MaxOutputTokens = 1024
        };

        Assert.Equal(config1, config2);
        Assert.NotEqual(config1, config3);
    }

    [Fact]
    public void RecordEquality_WithStopSequences_ComparesReference()
    {
        var stopSequences = new[] { "STOP" };
        var config1 = new GenerationConfig { StopSequences = stopSequences };
        var config2 = new GenerationConfig { StopSequences = stopSequences };
        var config3 = new GenerationConfig { StopSequences = new[] { "STOP" } };

        // Same reference - equal
        Assert.Equal(config1, config2);
        // Different reference (even with same content) - not equal due to reference comparison
        Assert.NotEqual(config1, config3);
    }

    [Fact]
    public void With_CreatesModifiedCopy()
    {
        var original = new GenerationConfig
        {
            Temperature = 0.5f,
            MaxOutputTokens = 512
        };

        var modified = original with { Temperature = 0.8f };

        Assert.Equal(0.5f, original.Temperature);
        Assert.Equal(0.8f, modified.Temperature);
        Assert.Equal(512, modified.MaxOutputTokens);
    }

    [Fact]
    public void PartialConfiguration_AllowsSelectiveSettings()
    {
        // Only set temperature, leave everything else null
        var config = new GenerationConfig { Temperature = 0.9f };

        Assert.Equal(0.9f, config.Temperature);
        Assert.Null(config.MaxOutputTokens);
        Assert.Null(config.TopP);
        Assert.Null(config.TopK);
        Assert.Null(config.StopSequences);
        Assert.Null(config.SystemInstruction);
    }
}
