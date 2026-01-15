namespace FireBlazor.Tests.Core;

/// <summary>
/// Tests for EmulatorOptions configuration.
/// </summary>
public class EmulatorOptionsTests
{
    [Fact]
    public void EmulatorOptions_InitializesWithNullHosts()
    {
        var options = new EmulatorOptions();

        Assert.Null(options.AuthHost);
        Assert.Null(options.FirestoreHost);
        Assert.Null(options.StorageHost);
        Assert.Null(options.RealtimeDatabaseHost);
        Assert.False(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_Auth_SetsHost()
    {
        var options = new EmulatorOptions().Auth("localhost:9099");

        Assert.Equal("localhost:9099", options.AuthHost);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_Firestore_SetsHost()
    {
        var options = new EmulatorOptions().Firestore("localhost:8080");

        Assert.Equal("localhost:8080", options.FirestoreHost);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_Storage_SetsHost()
    {
        var options = new EmulatorOptions().Storage("localhost:9199");

        Assert.Equal("localhost:9199", options.StorageHost);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_RealtimeDatabase_SetsHost()
    {
        var options = new EmulatorOptions().RealtimeDatabase("localhost:9000");

        Assert.Equal("localhost:9000", options.RealtimeDatabaseHost);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_All_ConfiguresAllServices()
    {
        var options = new EmulatorOptions()
            .All("localhost");

        Assert.Equal("localhost:9099", options.AuthHost);
        Assert.Equal("localhost:8080", options.FirestoreHost);
        Assert.Equal("localhost:9199", options.StorageHost);
        Assert.Equal("localhost:9000", options.RealtimeDatabaseHost);
        Assert.True(options.IsEnabled);
    }

    [Fact]
    public void EmulatorOptions_All_WithCustomPorts()
    {
        var options = new EmulatorOptions()
            .All("127.0.0.1", authPort: 9100, firestorePort: 8081, storagePort: 9200, databasePort: 9001);

        Assert.Equal("127.0.0.1:9100", options.AuthHost);
        Assert.Equal("127.0.0.1:8081", options.FirestoreHost);
        Assert.Equal("127.0.0.1:9200", options.StorageHost);
        Assert.Equal("127.0.0.1:9001", options.RealtimeDatabaseHost);
    }

    [Fact]
    public void EmulatorOptions_ChainsMethods()
    {
        var options = new EmulatorOptions()
            .Auth("localhost:9099")
            .Firestore("localhost:8080")
            .Storage("localhost:9199")
            .RealtimeDatabase("localhost:9000");

        Assert.Equal("localhost:9099", options.AuthHost);
        Assert.Equal("localhost:8080", options.FirestoreHost);
        Assert.Equal("localhost:9199", options.StorageHost);
        Assert.Equal("localhost:9000", options.RealtimeDatabaseHost);
    }

    [Fact]
    public void FirebaseOptions_UseEmulators_ConfiguresOptions()
    {
        var firebaseOptions = new FirebaseOptions()
            .WithProject("test-project")
            .UseEmulators(e => e
                .Auth("localhost:9099")
                .Firestore("localhost:8080"));

        Assert.NotNull(firebaseOptions.EmulatorOptions);
        Assert.Equal("localhost:9099", firebaseOptions.EmulatorOptions?.AuthHost);
        Assert.Equal("localhost:8080", firebaseOptions.EmulatorOptions?.FirestoreHost);
    }

    [Fact]
    public void EmulatorOptions_GetHostAndPort_ParsesCorrectly()
    {
        var options = new EmulatorOptions().Auth("localhost:9099");

        var result = options.GetAuthHostAndPort();

        Assert.NotNull(result);
        Assert.Equal("localhost", result.Value.Host);
        Assert.Equal(9099, result.Value.Port);
    }

    [Fact]
    public void EmulatorOptions_GetHostAndPort_ReturnsNull_WhenNotConfigured()
    {
        var options = new EmulatorOptions();

        var result = options.GetAuthHostAndPort();

        Assert.Null(result);
    }

    [Fact]
    public void EmulatorOptions_IsServiceEnabled_ChecksIndividualServices()
    {
        var options = new EmulatorOptions().Auth("localhost:9099");

        Assert.True(options.IsAuthEnabled);
        Assert.False(options.IsFirestoreEnabled);
        Assert.False(options.IsStorageEnabled);
        Assert.False(options.IsRealtimeDatabaseEnabled);
    }

    [Theory]
    [InlineData("localhost")]        // Missing port
    [InlineData(":9099")]            // Missing host
    [InlineData("localhost:abc")]    // Non-numeric port
    [InlineData("localhost:")]       // Empty port
    [InlineData(":")]                // Just colon
    public void EmulatorOptions_Auth_ThrowsForInvalidFormat(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.Auth(invalidHost));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmulatorOptions_Auth_ThrowsForNullOrWhitespace(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.Auth(invalidHost));
    }

    [Fact]
    public void EmulatorOptions_Auth_ThrowsForNull()
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentNullException>(() => options.Auth(null!));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:abc")]
    public void EmulatorOptions_Firestore_ThrowsForInvalidFormat(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.Firestore(invalidHost));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:abc")]
    public void EmulatorOptions_Storage_ThrowsForInvalidFormat(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.Storage(invalidHost));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:abc")]
    public void EmulatorOptions_RealtimeDatabase_ThrowsForInvalidFormat(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.RealtimeDatabase(invalidHost));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmulatorOptions_All_ThrowsForNullOrWhitespace(string invalidHost)
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentException>(() => options.All(invalidHost));
    }

    [Fact]
    public void EmulatorOptions_All_ThrowsForNull()
    {
        var options = new EmulatorOptions();
        Assert.Throws<ArgumentNullException>(() => options.All(null!));
    }

    [Fact]
    public void EmulatorOptions_GetAllHostAndPort_ParsesCorrectly()
    {
        var options = new EmulatorOptions()
            .Auth("localhost:9099")
            .Firestore("127.0.0.1:8080")
            .Storage("0.0.0.0:9199")
            .RealtimeDatabase("host.docker.internal:9000");

        var auth = options.GetAuthHostAndPort();
        var firestore = options.GetFirestoreHostAndPort();
        var storage = options.GetStorageHostAndPort();
        var database = options.GetRealtimeDatabaseHostAndPort();

        Assert.NotNull(auth);
        Assert.Equal("localhost", auth.Value.Host);
        Assert.Equal(9099, auth.Value.Port);

        Assert.NotNull(firestore);
        Assert.Equal("127.0.0.1", firestore.Value.Host);
        Assert.Equal(8080, firestore.Value.Port);

        Assert.NotNull(storage);
        Assert.Equal("0.0.0.0", storage.Value.Host);
        Assert.Equal(9199, storage.Value.Port);

        Assert.NotNull(database);
        Assert.Equal("host.docker.internal", database.Value.Host);
        Assert.Equal(9000, database.Value.Port);
    }
}
