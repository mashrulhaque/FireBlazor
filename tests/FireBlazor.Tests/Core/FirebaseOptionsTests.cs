namespace FireBlazor.Tests.Core;

public class FirebaseOptionsTests
{
    [Fact]
    public void FluentBuilder_SetsProjectId()
    {
        var options = new FirebaseOptions();
        options.WithProject("my-project");

        Assert.Equal("my-project", options.ProjectId);
    }

    [Fact]
    public void FluentBuilder_SetsApiKey()
    {
        var options = new FirebaseOptions();
        options.WithApiKey("AIzaSyTest123");

        Assert.Equal("AIzaSyTest123", options.ApiKey);
    }

    [Fact]
    public void FluentBuilder_ChainsCorrectly()
    {
        var options = new FirebaseOptions()
            .WithProject("test-project")
            .WithApiKey("test-key")
            .WithAuthDomain("test.firebaseapp.com")
            .WithStorageBucket("test.appspot.com")
            .WithDatabaseUrl("https://test.firebaseio.com");

        Assert.Equal("test-project", options.ProjectId);
        Assert.Equal("test-key", options.ApiKey);
        Assert.Equal("test.firebaseapp.com", options.AuthDomain);
        Assert.Equal("test.appspot.com", options.StorageBucket);
        Assert.Equal("https://test.firebaseio.com", options.DatabaseUrl);
    }

    [Fact]
    public void Validate_ThrowsWhenProjectIdMissing()
    {
        var options = new FirebaseOptions().WithApiKey("key");

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("ProjectId", ex.Message);
    }
}
