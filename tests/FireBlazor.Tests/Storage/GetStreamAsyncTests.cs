using FireBlazor.Testing;

namespace FireBlazor.Tests.Storage;

public class GetStreamAsyncTests
{
    [Fact]
    public async Task GetStreamAsync_ReturnsReadableStream()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/stream.txt");
        var content = "Stream content test";

        await reference.PutStringAsync(content);

        var result = await reference.GetStreamAsync();

        Assert.True(result.IsSuccess);
        using var reader = new StreamReader(result.Value);
        var readContent = await reader.ReadToEndAsync();
        Assert.Equal(content, readContent);
    }

    [Fact]
    public async Task GetStreamAsync_NonExistentFile_ReturnsError()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/nonexistent.txt");

        var result = await reference.GetStreamAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!.Message.ToLower());
    }

    [Fact]
    public async Task GetStreamAsync_LargeFile_RespectsMaxSize()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/large.bin");

        var largeContent = new string('x', 200);
        await reference.PutStringAsync(largeContent);

        var result = await reference.GetStreamAsync(maxSize: 100);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetStreamAsync_StreamIsSeekable()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/seekable.txt");

        await reference.PutStringAsync("seekable content");

        var result = await reference.GetStreamAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CanSeek);
        Assert.Equal(0, result.Value.Position);
    }
}
