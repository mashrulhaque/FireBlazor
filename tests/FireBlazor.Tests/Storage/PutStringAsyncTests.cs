using FireBlazor.Testing;

namespace FireBlazor.Tests.Storage;

public class PutStringAsyncTests
{
    [Fact]
    public async Task PutStringAsync_RawFormat_UploadsUtf8Bytes()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/hello.txt");

        var result = await reference.PutStringAsync("Hello, World!", StringFormat.Raw);

        Assert.True(result.IsSuccess);
        Assert.Equal("test/hello.txt", result.Value.FullPath);

        var bytes = await reference.GetBytesAsync();
        Assert.True(bytes.IsSuccess);
        Assert.Equal("Hello, World!", System.Text.Encoding.UTF8.GetString(bytes.Value));
    }

    [Fact]
    public async Task PutStringAsync_Base64Format_DecodesAndUploads()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/data.bin");
        var originalData = new byte[] { 1, 2, 3, 4, 5 };
        var base64 = Convert.ToBase64String(originalData);

        var result = await reference.PutStringAsync(base64, StringFormat.Base64);

        Assert.True(result.IsSuccess);

        var bytes = await reference.GetBytesAsync();
        Assert.True(bytes.IsSuccess);
        Assert.Equal(originalData, bytes.Value);
    }

    [Fact]
    public async Task PutStringAsync_WithMetadata_StoresMetadata()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/doc.json");
        var metadata = new StorageMetadata { ContentType = "application/json" };

        await reference.PutStringAsync("{\"key\":\"value\"}", StringFormat.Raw, metadata);

        var metadataResult = await reference.GetMetadataAsync();
        Assert.True(metadataResult.IsSuccess);
        Assert.Equal("application/json", metadataResult.Value.ContentType);
    }

    [Fact]
    public async Task PutStringAsync_WithProgress_ReportsProgress()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/progress.txt");
        var progressReported = false;

        await reference.PutStringAsync("Test data", StringFormat.Raw, onProgress: progress =>
        {
            progressReported = true;
            Assert.True(progress.BytesTransferred > 0);
        });

        Assert.True(progressReported);
    }

    [Fact]
    public async Task PutStringAsync_WithCancellation_ThrowsOperationCanceled()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/cancel.txt");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reference.PutStringAsync("data", cancellationToken: cts.Token));
    }
}
