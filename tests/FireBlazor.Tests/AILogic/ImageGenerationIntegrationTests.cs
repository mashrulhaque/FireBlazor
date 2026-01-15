using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

/// <summary>
/// Integration tests for image generation features using FakeFirebaseAI.
/// </summary>
public class ImageGenerationIntegrationTests
{
    [Fact]
    public void GetImageModel_ReturnsFakeImageModel()
    {
        var firebaseAI = new FakeFirebaseAI();

        var model = firebaseAI.GetImageModel("imagen-4.0-generate-001");

        Assert.NotNull(model);
        Assert.Equal("imagen-4.0-generate-001", model.ModelName);
    }

    [Fact]
    public void GetImageModel_UsesDefaultModelName()
    {
        var firebaseAI = new FakeFirebaseAI();

        var model = firebaseAI.GetImageModel();

        Assert.NotNull(model);
        Assert.Equal("imagen-4.0-generate-001", model.ModelName);
    }

    [Fact]
    public async Task GenerateImages_EndToEndWorkflow()
    {
        // Arrange: Get AI service and model
        var firebaseAI = new FakeFirebaseAI();
        var model = firebaseAI.GetImageModel();

        // Act: Generate images
        var result = await model.GenerateImagesAsync("A beautiful sunset over mountains");

        // Assert: Workflow completes successfully
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Images);
    }

    [Fact]
    public async Task GenerateImages_WithConfig_PassesConfigThrough()
    {
        var firebaseAI = new FakeFirebaseAI();
        var model = (FakeImageModel)firebaseAI.GetImageModel();

        var config = new ImageGenerationConfig
        {
            NumberOfImages = 4,
            AspectRatio = "16:9",
            NegativePrompt = "blurry, low quality"
        };

        await model.GenerateImagesAsync("A forest scene", config);

        Assert.Single(model.Calls);
        Assert.Equal("A forest scene", model.Calls[0].Prompt);
        Assert.Equal(4, model.Calls[0].Config?.NumberOfImages);
        Assert.Equal("16:9", model.Calls[0].Config?.AspectRatio);
        Assert.Equal("blurry, low quality", model.Calls[0].Config?.NegativePrompt);
    }

    [Fact]
    public async Task GenerateImages_FirstImageHelper_ReturnsFirstImage()
    {
        var firebaseAI = new FakeFirebaseAI();
        var model = (FakeImageModel)firebaseAI.GetImageModel();

        var customResponse = new ImageGenerationResponse
        {
            Images =
            [
                new GeneratedImage { Base64Data = "first", MimeType = "image/png" },
                new GeneratedImage { Base64Data = "second", MimeType = "image/png" }
            ]
        };
        model.SetupResponse(customResponse);

        var result = await model.GenerateImagesAsync("Multiple images");

        Assert.True(result.IsSuccess);
        Assert.Equal("first", result.Value.FirstImage?.Base64Data);
        Assert.Equal(2, result.Value.Images.Count);
    }

    [Fact]
    public async Task GenerateImages_ToDataUrl_ReturnsValidDataUrl()
    {
        var firebaseAI = new FakeFirebaseAI();
        var model = firebaseAI.GetImageModel();

        var result = await model.GenerateImagesAsync("Test image");

        Assert.True(result.IsSuccess);
        var image = result.Value.Images[0];
        var dataUrl = image.ToDataUrl();

        Assert.StartsWith("data:image/png;base64,", dataUrl);
    }

    [Fact]
    public async Task MultipleModelInstances_WorkIndependently()
    {
        var firebaseAI = new FakeFirebaseAI();

        var model1 = (FakeImageModel)firebaseAI.GetImageModel("model-1");
        var model2 = (FakeImageModel)firebaseAI.GetImageModel("model-2");

        await model1.GenerateImagesAsync("Prompt for model 1");
        await model2.GenerateImagesAsync("Prompt for model 2");
        await model1.GenerateImagesAsync("Another prompt for model 1");

        Assert.Equal(2, model1.Calls.Count);
        Assert.Single(model2.Calls);
        Assert.Equal("model-1", model1.ModelName);
        Assert.Equal("model-2", model2.ModelName);
    }
}
