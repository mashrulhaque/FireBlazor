using FireBlazor.Testing;

namespace FireBlazor.Tests.Storage;

public class UpdateMetadataAsyncTests
{
    [Fact]
    public async Task UpdateMetadataAsync_UpdatesExistingFile()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/file.txt");

        await reference.PutStringAsync("content", metadata: new StorageMetadata { ContentType = "text/plain" });

        var newMetadata = new StorageMetadata
        {
            ContentType = "text/html",
            CacheControl = "max-age=3600"
        };
        var result = await reference.UpdateMetadataAsync(newMetadata);

        Assert.True(result.IsSuccess);
        Assert.Equal("text/html", result.Value.ContentType);
        Assert.Equal("max-age=3600", result.Value.CacheControl);
    }

    [Fact]
    public async Task UpdateMetadataAsync_NonExistentFile_ReturnsError()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/nonexistent.txt");

        var result = await reference.UpdateMetadataAsync(new StorageMetadata { ContentType = "text/plain" });

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!.Message.ToLower());
    }

    [Fact]
    public async Task UpdateMetadataAsync_WithCustomMetadata_UpdatesCustomFields()
    {
        var storage = new FakeFirebaseStorage();
        var reference = storage.Ref("test/custom.txt");

        await reference.PutStringAsync("content");

        var result = await reference.UpdateMetadataAsync(new StorageMetadata
        {
            CustomMetadata = new Dictionary<string, string>
            {
                ["author"] = "test",
                ["version"] = "1.0"
            }
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.CustomMetadata);
        Assert.Equal("test", result.Value.CustomMetadata["author"]);
    }
}
