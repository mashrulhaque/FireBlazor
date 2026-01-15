using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Core;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFirebase_RegistersIFirebase()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());

        services.AddFirebase(options => options
            .WithProject("test-project")
            .WithApiKey("test-key"));

        var provider = services.BuildServiceProvider();
        var firebase = provider.GetService<IFirebase>();

        Assert.NotNull(firebase);
    }

    [Fact]
    public void AddFirebase_ConfiguresOptions()
    {
        var services = new ServiceCollection();

        services.AddFirebase(options => options
            .WithProject("my-project")
            .WithApiKey("my-key")
            .UseAuth()
            .UseFirestore());

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<FirebaseOptions>();

        Assert.Equal("my-project", options.ProjectId);
        Assert.NotNull(options.AuthOptions);
        Assert.NotNull(options.FirestoreOptions);
    }

    [Fact]
    public void AddFirebase_ThrowsWithoutProjectId()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());

        services.AddFirebase(options => options.WithApiKey("key"));

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IFirebase>());
    }
}
