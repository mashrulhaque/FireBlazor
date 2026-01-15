namespace FireBlazor.Tests.AILogic;

public class ImageGenerationResponseTests
{
    [Fact]
    public void ImageGenerationResponse_RequiresImages()
    {
        var images = new List<GeneratedImage>
        {
            new() { Base64Data = "data1", MimeType = "image/png" }
        };

        var response = new ImageGenerationResponse { Images = images };

        Assert.Single(response.Images);
    }

    [Fact]
    public void ImageGenerationResponse_CanContainMultipleImages()
    {
        var images = new List<GeneratedImage>
        {
            new() { Base64Data = "data1", MimeType = "image/png" },
            new() { Base64Data = "data2", MimeType = "image/png" },
            new() { Base64Data = "data3", MimeType = "image/png" },
            new() { Base64Data = "data4", MimeType = "image/png" }
        };

        var response = new ImageGenerationResponse { Images = images };

        Assert.Equal(4, response.Images.Count);
    }

    [Fact]
    public void ImageGenerationResponse_FirstImage_ReturnsFirstOrDefault()
    {
        var images = new List<GeneratedImage>
        {
            new() { Base64Data = "first", MimeType = "image/png" },
            new() { Base64Data = "second", MimeType = "image/png" }
        };

        var response = new ImageGenerationResponse { Images = images };

        Assert.Equal("first", response.FirstImage?.Base64Data);
    }

    [Fact]
    public void ImageGenerationResponse_FirstImage_ReturnsNullWhenEmpty()
    {
        var response = new ImageGenerationResponse { Images = [] };

        Assert.Null(response.FirstImage);
    }
}
