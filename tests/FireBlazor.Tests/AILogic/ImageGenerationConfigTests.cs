namespace FireBlazor.Tests.AILogic;

public class ImageGenerationConfigTests
{
    [Fact]
    public void ImageGenerationConfig_DefaultValues_AreNull()
    {
        var config = new ImageGenerationConfig();

        Assert.Null(config.NumberOfImages);
        Assert.Null(config.AspectRatio);
        Assert.Null(config.NegativePrompt);
    }

    [Fact]
    public void ImageGenerationConfig_WithValues_SetsProperties()
    {
        var config = new ImageGenerationConfig
        {
            NumberOfImages = 4,
            AspectRatio = "16:9",
            NegativePrompt = "blurry, low quality"
        };

        Assert.Equal(4, config.NumberOfImages);
        Assert.Equal("16:9", config.AspectRatio);
        Assert.Equal("blurry, low quality", config.NegativePrompt);
    }

    [Theory]
    [InlineData("1:1")]
    [InlineData("16:9")]
    [InlineData("9:16")]
    [InlineData("4:3")]
    [InlineData("3:4")]
    public void ImageGenerationConfig_SupportsCommonAspectRatios(string aspectRatio)
    {
        var config = new ImageGenerationConfig { AspectRatio = aspectRatio };
        Assert.Equal(aspectRatio, config.AspectRatio);
    }
}
