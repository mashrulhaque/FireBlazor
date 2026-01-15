using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

public class FakeImageModelTests
{
    [Fact]
    public void Constructor_SetsModelName()
    {
        var model = new FakeImageModel("test-model");
        Assert.Equal("test-model", model.ModelName);
    }

    [Fact]
    public void Constructor_UsesDefaultModelName()
    {
        var model = new FakeImageModel();
        Assert.Equal("fake-imagen-model", model.ModelName);
    }

    [Fact]
    public async Task GenerateImagesAsync_ReturnsDefaultResponse_WhenNoSetup()
    {
        var model = new FakeImageModel();

        var result = await model.GenerateImagesAsync("A cat");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Images);
        Assert.Equal("image/png", result.Value.Images[0].MimeType);
    }

    [Fact]
    public async Task GenerateImagesAsync_ReturnsSetupResponse()
    {
        var model = new FakeImageModel();
        var expectedResponse = new ImageGenerationResponse
        {
            Images = [
                new GeneratedImage { Base64Data = "custom", MimeType = "image/jpeg" }
            ]
        };
        model.SetupResponse(expectedResponse);

        var result = await model.GenerateImagesAsync("A dog");

        Assert.True(result.IsSuccess);
        Assert.Equal("custom", result.Value.Images[0].Base64Data);
        Assert.Equal("image/jpeg", result.Value.Images[0].MimeType);
    }

    [Fact]
    public async Task GenerateImagesAsync_ReturnsSetupError()
    {
        var model = new FakeImageModel();
        model.SetupError("ai/test-error", "Test error message");

        var result = await model.GenerateImagesAsync("A bird");

        Assert.True(result.IsFailure);
        Assert.Equal("ai/test-error", result.Error!.Code);
        Assert.Equal("Test error message", result.Error.Message);
    }

    [Fact]
    public async Task GenerateImagesAsync_TracksAllCalls()
    {
        var model = new FakeImageModel();
        var config1 = new ImageGenerationConfig { NumberOfImages = 2 };
        var config2 = new ImageGenerationConfig { AspectRatio = "16:9" };

        await model.GenerateImagesAsync("First prompt", config1);
        await model.GenerateImagesAsync("Second prompt", config2);
        await model.GenerateImagesAsync("Third prompt");

        Assert.Equal(3, model.Calls.Count);
        Assert.Equal("First prompt", model.Calls[0].Prompt);
        Assert.Equal(2, model.Calls[0].Config?.NumberOfImages);
        Assert.Equal("Second prompt", model.Calls[1].Prompt);
        Assert.Equal("16:9", model.Calls[1].Config?.AspectRatio);
        Assert.Equal("Third prompt", model.Calls[2].Prompt);
        Assert.Null(model.Calls[2].Config);
    }

    [Fact]
    public async Task GenerateImagesAsync_DequeuesmultipleResponses()
    {
        var model = new FakeImageModel();

        var response1 = new ImageGenerationResponse
        {
            Images = [new GeneratedImage { Base64Data = "first", MimeType = "image/png" }]
        };
        var response2 = new ImageGenerationResponse
        {
            Images = [new GeneratedImage { Base64Data = "second", MimeType = "image/jpeg" }]
        };

        model.SetupResponse(response1);
        model.SetupResponse(response2);

        var result1 = await model.GenerateImagesAsync("Prompt 1");
        var result2 = await model.GenerateImagesAsync("Prompt 2");
        var result3 = await model.GenerateImagesAsync("Prompt 3"); // Should get default

        Assert.Equal("first", result1.Value.Images[0].Base64Data);
        Assert.Equal("second", result2.Value.Images[0].Base64Data);
        Assert.NotEqual("first", result3.Value.Images[0].Base64Data);
        Assert.NotEqual("second", result3.Value.Images[0].Base64Data);
    }
}
