using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

public class GenerativeModelTests
{
    [Fact]
    public async Task CountTokensAsync_WithText_ReturnsTokenCount()
    {
        // Arrange
        var fakeAI = new FakeFirebaseAI();
        var model = fakeAI.GetModel("gemini-2.5-flash");

        // Act
        var result = await model.CountTokensAsync("Hello world this is a test");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TotalTokens > 0);
        Assert.NotNull(result.Value.TextTokens);
    }

    [Fact]
    public async Task CountTokensAsync_WithMultimodalContent_ReturnsTokenCount()
    {
        // Arrange
        var fakeAI = new FakeFirebaseAI();
        var model = fakeAI.GetModel("gemini-2.5-flash");
        var parts = new ContentPart[]
        {
            ContentPart.Text("Describe this image"),
            ContentPart.Image(new byte[] { 1, 2, 3 }, "image/png")
        };

        // Act
        var result = await model.CountTokensAsync(parts);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TotalTokens > 0);
        Assert.NotNull(result.Value.TextTokens);
        Assert.NotNull(result.Value.ImageTokens);
    }

    [Fact]
    public async Task CountTokensAsync_EmptyText_ReturnsZeroTokens()
    {
        // Arrange
        var fakeAI = new FakeFirebaseAI();
        var model = fakeAI.GetModel("gemini-2.5-flash");

        // Act
        var result = await model.CountTokensAsync("");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalTokens);
    }
}
