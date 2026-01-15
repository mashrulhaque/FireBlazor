namespace FireBlazor.Tests.AILogic;

public class GeneratedImageTests
{
    [Fact]
    public void GeneratedImage_RequiresBase64Data()
    {
        var image = new GeneratedImage
        {
            Base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
            MimeType = "image/png"
        };

        Assert.NotEmpty(image.Base64Data);
    }

    [Fact]
    public void GeneratedImage_RequiresMimeType()
    {
        var image = new GeneratedImage
        {
            Base64Data = "base64data",
            MimeType = "image/png"
        };

        Assert.Equal("image/png", image.MimeType);
    }

    [Fact]
    public void GeneratedImage_ToDataUrl_ReturnsValidDataUrl()
    {
        var image = new GeneratedImage
        {
            Base64Data = "iVBORw0KGgoAAAANSUhEUg==",
            MimeType = "image/png"
        };

        var dataUrl = image.ToDataUrl();

        Assert.Equal("data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==", dataUrl);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    public void GeneratedImage_SupportedMimeTypes(string mimeType)
    {
        var image = new GeneratedImage
        {
            Base64Data = "data",
            MimeType = mimeType
        };

        Assert.Equal(mimeType, image.MimeType);
    }
}
