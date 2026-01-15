namespace FireBlazor.Tests.AILogic;

public class ContentPartTests
{
    [Fact]
    public void ContentPart_IsAbstractRecord()
    {
        // ContentPart should be abstract - cannot instantiate directly
        var type = typeof(ContentPart);
        Assert.True(type.IsAbstract);
    }

    [Fact]
    public void TextPart_StoresText()
    {
        var part = new TextPart("Hello, world!");

        Assert.Equal("Hello, world!", part.Text);
    }

    [Fact]
    public void TextPart_InheritsFromContentPart()
    {
        var part = new TextPart("test");

        Assert.IsAssignableFrom<ContentPart>(part);
    }

    [Fact]
    public void TextPart_SupportsRecordEquality()
    {
        var part1 = new TextPart("hello");
        var part2 = new TextPart("hello");
        var part3 = new TextPart("world");

        Assert.Equal(part1, part2);
        Assert.NotEqual(part1, part3);
    }

    [Fact]
    public void ImagePart_StoresDataAndMimeType()
    {
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var part = new ImagePart(imageData, "image/png");

        Assert.Equal(imageData, part.Data);
        Assert.Equal("image/png", part.MimeType);
    }

    [Fact]
    public void ImagePart_InheritsFromContentPart()
    {
        var part = new ImagePart(new byte[] { 1, 2, 3 }, "image/jpeg");

        Assert.IsAssignableFrom<ContentPart>(part);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public void ImagePart_AcceptsValidImageMimeTypes(string mimeType)
    {
        var part = new ImagePart(new byte[] { 1 }, mimeType);

        Assert.Equal(mimeType, part.MimeType);
    }

    [Fact]
    public void Base64ImagePart_StoresBase64DataAndMimeType()
    {
        var base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var part = new Base64ImagePart(base64Data, "image/png");

        Assert.Equal(base64Data, part.Base64Data);
        Assert.Equal("image/png", part.MimeType);
    }

    [Fact]
    public void Base64ImagePart_InheritsFromContentPart()
    {
        var part = new Base64ImagePart("abc123", "image/jpeg");

        Assert.IsAssignableFrom<ContentPart>(part);
    }

    [Fact]
    public void FileUriPart_StoresUriAndMimeType()
    {
        var uri = "gs://my-bucket/images/photo.jpg";
        var part = new FileUriPart(uri, "image/jpeg");

        Assert.Equal(uri, part.Uri);
        Assert.Equal("image/jpeg", part.MimeType);
    }

    [Fact]
    public void FileUriPart_InheritsFromContentPart()
    {
        var part = new FileUriPart("gs://bucket/file.pdf", "application/pdf");

        Assert.IsAssignableFrom<ContentPart>(part);
    }

    [Theory]
    [InlineData("gs://bucket/image.png", "image/png")]
    [InlineData("gs://bucket/doc.pdf", "application/pdf")]
    [InlineData("https://storage.googleapis.com/bucket/video.mp4", "video/mp4")]
    public void FileUriPart_AcceptsVariousUriFormats(string uri, string mimeType)
    {
        var part = new FileUriPart(uri, mimeType);

        Assert.Equal(uri, part.Uri);
        Assert.Equal(mimeType, part.MimeType);
    }

    [Fact]
    public void ContentPart_Text_CreatesTextPart()
    {
        ContentPart part = ContentPart.Text("Hello!");

        Assert.IsType<TextPart>(part);
        Assert.Equal("Hello!", ((TextPart)part).Text);
    }

    [Fact]
    public void ContentPart_Image_WithBytes_CreatesImagePart()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        ContentPart part = ContentPart.Image(bytes, "image/png");

        Assert.IsType<ImagePart>(part);
        var imagePart = (ImagePart)part;
        Assert.Equal(bytes, imagePart.Data);
        Assert.Equal("image/png", imagePart.MimeType);
    }

    [Fact]
    public void ContentPart_Image_WithBase64_CreatesBase64ImagePart()
    {
        var base64 = "iVBORw0KGgoAAAANSUhEUg==";
        ContentPart part = ContentPart.FromBase64(base64, "image/png");

        Assert.IsType<Base64ImagePart>(part);
        var imagePart = (Base64ImagePart)part;
        Assert.Equal(base64, imagePart.Base64Data);
        Assert.Equal("image/png", imagePart.MimeType);
    }

    [Fact]
    public void ContentPart_FileUri_CreatesFileUriPart()
    {
        ContentPart part = ContentPart.FileUri("gs://bucket/file.pdf", "application/pdf");

        Assert.IsType<FileUriPart>(part);
        var filePart = (FileUriPart)part;
        Assert.Equal("gs://bucket/file.pdf", filePart.Uri);
        Assert.Equal("application/pdf", filePart.MimeType);
    }
}
