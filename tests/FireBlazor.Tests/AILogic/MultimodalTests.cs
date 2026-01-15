using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

public class MultimodalTests
{
    [Fact]
    public void ContentPart_CanBuildMixedContentArray()
    {
        // Simulate a typical vision request
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        var parts = new ContentPart[]
        {
            ContentPart.Text("First, look at this image:"),
            ContentPart.Image(imageBytes, "image/png"),
            ContentPart.Text("Now describe what you see.")
        };

        Assert.Equal(3, parts.Length);
        Assert.IsType<TextPart>(parts[0]);
        Assert.IsType<ImagePart>(parts[1]);
        Assert.IsType<TextPart>(parts[2]);
    }

    [Fact]
    public void ContentPart_SupportsMultipleImages()
    {
        var parts = new ContentPart[]
        {
            ContentPart.Text("Compare these two images:"),
            ContentPart.Image(new byte[] { 1, 2, 3 }, "image/png"),
            ContentPart.Image(new byte[] { 4, 5, 6 }, "image/jpeg"),
            ContentPart.Text("Which one is better?")
        };

        var images = parts.OfType<ImagePart>().ToList();
        Assert.Equal(2, images.Count);
        Assert.Equal("image/png", images[0].MimeType);
        Assert.Equal("image/jpeg", images[1].MimeType);
    }

    [Fact]
    public void ContentPart_SupportsPdfViaFileUri()
    {
        var parts = new ContentPart[]
        {
            ContentPart.FileUri("gs://my-bucket/documents/report.pdf", "application/pdf"),
            ContentPart.Text("Summarize this PDF document.")
        };

        var filePart = parts.OfType<FileUriPart>().Single();
        Assert.Equal("application/pdf", filePart.MimeType);
        Assert.StartsWith("gs://", filePart.Uri);
    }

    [Fact]
    public async Task FakeModel_HandlesMultimodalInput()
    {
        var fakeAI = new FakeFirebaseAI();
        fakeAI.ConfigureDefaultResponse("This image shows a beautiful landscape with mountains.");

        var model = fakeAI.GetModel("gemini-2.5-flash");

        var parts = new ContentPart[]
        {
            ContentPart.Image(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A }, "image/png"),
            ContentPart.Text("Describe this landscape photo in detail.")
        };

        var result = await model.GenerateContentAsync(parts);

        Assert.True(result.IsSuccess);
        Assert.Contains("landscape", result.Value!.Text);
        Assert.Contains("mountains", result.Value.Text);
    }

    [Fact]
    public async Task FakeChat_HandlesMultimodalConversation()
    {
        var fakeAI = new FakeFirebaseAI();
        fakeAI.ConfigureDefaultResponse("I see a red sports car.");

        var model = fakeAI.GetModel("gemini-2.5-flash");
        var chat = model.StartChat();

        // First message with image
        var result1 = await chat.SendMessageAsync(new ContentPart[]
        {
            ContentPart.Image(new byte[] { 1, 2, 3 }, "image/jpeg"),
            ContentPart.Text("What is in this image?")
        });

        // Follow-up text question (same response since ConfigureDefaultResponse sets static response)
        var result2 = await chat.SendMessageAsync("What brand is it?");

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(4, chat.History.Count);
        Assert.Equal("I see a red sports car.", result1.Value!.Text);
        Assert.Equal("I see a red sports car.", result2.Value!.Text);
    }

    [Fact]
    public void ImagePart_ConvertsToBase64Correctly()
    {
        var originalBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var imagePart = new ImagePart(originalBytes, "image/png");

        var base64 = Convert.ToBase64String(imagePart.Data);
        var roundTripped = Convert.FromBase64String(base64);

        Assert.Equal(originalBytes, roundTripped);
    }

    [Fact]
    public void Base64ImagePart_StoresDataWithoutPrefix()
    {
        // Data URL format: data:image/png;base64,ABC123...
        // Base64ImagePart should store just: ABC123...
        var pureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        var part = ContentPart.FromBase64(pureBase64, "image/png");
        var base64Part = (Base64ImagePart)part;

        Assert.DoesNotContain("data:", base64Part.Base64Data);
        Assert.DoesNotContain("base64,", base64Part.Base64Data);
        Assert.Equal(pureBase64, base64Part.Base64Data);
    }
}
