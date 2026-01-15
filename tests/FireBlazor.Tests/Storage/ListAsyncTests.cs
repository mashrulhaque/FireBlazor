using FireBlazor.Testing;

namespace FireBlazor.Tests.Storage;

public class ListAsyncTests
{
    [Fact]
    public async Task ListAsync_ReturnsItemsInDirectory()
    {
        var storage = new FakeFirebaseStorage();

        await storage.Ref("folder/file1.txt").PutStringAsync("content1");
        await storage.Ref("folder/file2.txt").PutStringAsync("content2");
        await storage.Ref("folder/file3.txt").PutStringAsync("content3");

        var result = await storage.Ref("folder").ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count);
    }

    [Fact]
    public async Task ListAsync_WithMaxResults_LimitsItems()
    {
        var storage = new FakeFirebaseStorage();

        for (int i = 0; i < 10; i++)
        {
            await storage.Ref($"folder/file{i}.txt").PutStringAsync($"content{i}");
        }

        var result = await storage.Ref("folder").ListAsync(new ListOptions { MaxResults = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.NotNull(result.Value.NextPageToken);
    }

    [Fact]
    public async Task ListAsync_WithPageToken_ReturnNextPage()
    {
        var storage = new FakeFirebaseStorage();

        for (int i = 0; i < 5; i++)
        {
            await storage.Ref($"folder/file{i}.txt").PutStringAsync($"content{i}");
        }

        var page1 = await storage.Ref("folder").ListAsync(new ListOptions { MaxResults = 2 });
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value.NextPageToken);

        var page2 = await storage.Ref("folder").ListAsync(new ListOptions
        {
            MaxResults = 2,
            PageToken = page1.Value.NextPageToken
        });

        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Value.Items.Count);
    }

    [Fact]
    public async Task ListAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var storage = new FakeFirebaseStorage();

        var result = await storage.Ref("empty-folder").ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Null(result.Value.NextPageToken);
    }
}
