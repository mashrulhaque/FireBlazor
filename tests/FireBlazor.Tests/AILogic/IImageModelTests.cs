namespace FireBlazor.Tests.AILogic;

public class IImageModelTests
{
    [Fact]
    public void IImageModel_HasModelNameProperty()
    {
        var model = new MockImageModel("imagen-4.0-generate-001");

        Assert.Equal("imagen-4.0-generate-001", model.ModelName);
    }

    [Fact]
    public async Task IImageModel_GenerateImagesAsync_ReturnsResult()
    {
        var model = new MockImageModel("imagen-4.0-generate-001");

        var result = await model.GenerateImagesAsync("A sunset over mountains");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task IImageModel_GenerateImagesAsync_AcceptsConfig()
    {
        var model = new MockImageModel("imagen-4.0-generate-001");
        var config = new ImageGenerationConfig
        {
            NumberOfImages = 2,
            AspectRatio = "16:9"
        };

        var result = await model.GenerateImagesAsync("A sunset", config);

        Assert.True(result.IsSuccess);
    }

    private class MockImageModel : IImageModel
    {
        public string ModelName { get; }

        public MockImageModel(string modelName)
        {
            ModelName = modelName;
        }

        public Task<Result<ImageGenerationResponse>> GenerateImagesAsync(
            string prompt,
            ImageGenerationConfig? config = null,
            CancellationToken cancellationToken = default)
        {
            var response = new ImageGenerationResponse
            {
                Images = [new GeneratedImage { Base64Data = "mock", MimeType = "image/png" }]
            };
            return Task.FromResult(Result<ImageGenerationResponse>.Success(response));
        }
    }
}
