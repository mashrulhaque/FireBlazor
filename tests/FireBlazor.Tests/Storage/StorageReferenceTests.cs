namespace FireBlazor.Tests.Storage;

public class StorageReferenceTests
{
    [Fact]
    public void StorageMetadata_DefaultValues()
    {
        var metadata = new StorageMetadata();

        Assert.Null(metadata.ContentType);
        Assert.Null(metadata.CacheControl);
        Assert.Null(metadata.ContentDisposition);
        Assert.Null(metadata.ContentEncoding);
        Assert.Null(metadata.ContentLanguage);
        Assert.Null(metadata.CustomMetadata);
    }

    [Fact]
    public void StorageMetadata_SetsAllProperties()
    {
        var metadata = new StorageMetadata
        {
            ContentType = "image/png",
            CacheControl = "max-age=3600",
            ContentDisposition = "inline",
            ContentEncoding = "gzip",
            ContentLanguage = "en-US",
            CustomMetadata = new Dictionary<string, string> { ["author"] = "test" }
        };

        Assert.Equal("image/png", metadata.ContentType);
        Assert.Equal("max-age=3600", metadata.CacheControl);
        Assert.Equal("inline", metadata.ContentDisposition);
        Assert.Equal("gzip", metadata.ContentEncoding);
        Assert.Equal("en-US", metadata.ContentLanguage);
        Assert.Equal("test", metadata.CustomMetadata["author"]);
    }

    [Fact]
    public void UploadResult_RequiredProperties()
    {
        var result = new UploadResult
        {
            DownloadUrl = "https://storage.googleapis.com/bucket/test.jpg",
            FullPath = "images/test.jpg",
            BytesTransferred = 1024
        };

        Assert.Equal("https://storage.googleapis.com/bucket/test.jpg", result.DownloadUrl);
        Assert.Equal("images/test.jpg", result.FullPath);
        Assert.Equal(1024, result.BytesTransferred);
    }

    [Fact]
    public void UploadProgress_CalculatesPercentage()
    {
        var progress = new UploadProgress
        {
            BytesTransferred = 500,
            TotalBytes = 1000
        };

        Assert.Equal(50.0, progress.Percentage);
    }

    [Fact]
    public void UploadProgress_ZeroTotalBytes_ReturnsZeroPercentage()
    {
        var progress = new UploadProgress
        {
            BytesTransferred = 0,
            TotalBytes = 0
        };

        Assert.Equal(0.0, progress.Percentage);
    }

    [Fact]
    public void ListResult_DefaultValues()
    {
        var result = new ListResult();

        Assert.Empty(result.Items);
        Assert.Empty(result.Prefixes);
    }
}
