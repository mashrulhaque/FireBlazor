using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

/// <summary>
/// Tests for FakeFirebaseStorage and FakeRealtimeDatabase test doubles.
/// </summary>
public class FakeFirebaseStorageTests
{
    [Fact]
    public async Task Storage_PutAsync_StoresData()
    {
        var storage = new FakeFirebaseStorage();
        var data = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(data);

        var result = await storage.Ref("files/test.txt").PutAsync(stream, new StorageMetadata { ContentType = "text/plain" });

        Assert.True(result.IsSuccess);
        Assert.Equal("files/test.txt", result.Value.FullPath);
        Assert.Equal(data.Length, result.Value.BytesTransferred);
    }

    [Fact]
    public async Task Storage_GetBytesAsync_RetrievesData()
    {
        var storage = new FakeFirebaseStorage();
        var originalData = "Test content"u8.ToArray();
        using var stream = new MemoryStream(originalData);
        await storage.Ref("files/test.txt").PutAsync(stream);

        var result = await storage.Ref("files/test.txt").GetBytesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(originalData, result.Value);
    }

    [Fact]
    public async Task Storage_GetDownloadUrlAsync_ReturnsUrl()
    {
        var storage = new FakeFirebaseStorage();
        using var stream = new MemoryStream([1, 2, 3]);
        await storage.Ref("images/photo.jpg").PutAsync(stream);

        var result = await storage.Ref("images/photo.jpg").GetDownloadUrlAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains("images/photo.jpg", result.Value);
    }

    [Fact]
    public async Task Storage_DeleteAsync_RemovesFile()
    {
        var storage = new FakeFirebaseStorage();
        using var stream = new MemoryStream([1, 2, 3]);
        await storage.Ref("files/delete-me.txt").PutAsync(stream);

        var deleteResult = await storage.Ref("files/delete-me.txt").DeleteAsync();
        var getResult = await storage.Ref("files/delete-me.txt").GetBytesAsync();

        Assert.True(deleteResult.IsSuccess);
        Assert.True(getResult.IsFailure);
        Assert.Equal("storage/object-not-found", getResult.Error?.Code);
    }

    [Fact]
    public async Task Storage_ListAllAsync_ReturnsItems()
    {
        var storage = new FakeFirebaseStorage();
        using var stream1 = new MemoryStream([1]);
        using var stream2 = new MemoryStream([2]);
        await storage.Ref("folder/file1.txt").PutAsync(stream1);
        await storage.Ref("folder/file2.txt").PutAsync(stream2);

        var result = await storage.Ref("folder").ListAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Storage_ReportsProgress()
    {
        var storage = new FakeFirebaseStorage();
        var data = new byte[1000];
        using var stream = new MemoryStream(data);
        var progressReports = new List<UploadProgress>();

        await storage.Ref("files/large.bin").PutAsync(stream, onProgress: p => progressReports.Add(p));

        Assert.NotEmpty(progressReports);
        Assert.Equal(100.0, progressReports.Last().Percentage);
    }

    [Fact]
    public void Storage_SimulateError_CausesFailure()
    {
        var storage = new FakeFirebaseStorage();
        storage.SimulateError(new FirebaseError("storage/unauthorized", "Not authorized"));

        var result = storage.Ref("files/test.txt").GetBytesAsync().Result;

        Assert.True(result.IsFailure);
        Assert.Equal("storage/unauthorized", result.Error?.Code);
    }

    [Fact]
    public void Storage_Child_NavigatesCorrectly()
    {
        var storage = new FakeFirebaseStorage();
        var parent = storage.Ref("root");
        var child = parent.Child("subfolder").Child("file.txt");

        Assert.Equal("root/subfolder/file.txt", child.FullPath);
        Assert.Equal("file.txt", child.Name);
    }

    [Fact]
    public void Storage_Parent_NavigatesUp()
    {
        var storage = new FakeFirebaseStorage();
        var file = storage.Ref("root/subfolder/file.txt");

        Assert.Equal("root/subfolder", file.Parent?.FullPath);
        Assert.Equal("root", file.Parent?.Parent?.FullPath);
    }
}

/// <summary>
/// Tests for FakeRealtimeDatabase test double.
/// </summary>
public class FakeRealtimeDatabaseTests
{
    public class TestData
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    [Fact]
    public async Task Database_SetAsync_StoresData()
    {
        var database = new FakeRealtimeDatabase();

        var result = await database.Ref("items/item1").SetAsync(new TestData { Name = "Test", Value = 42 });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Database_GetAsync_RetrievesData()
    {
        var database = new FakeRealtimeDatabase();
        await database.Ref("items/item1").SetAsync(new TestData { Name = "Test", Value = 42 });

        var result = await database.Ref("items/item1").GetAsync<TestData>();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Exists);
        Assert.Equal("Test", result.Value.Value?.Name);
        Assert.Equal(42, result.Value.Value?.Value);
    }

    [Fact]
    public async Task Database_GetAsync_ReturnsNotExists_WhenMissing()
    {
        var database = new FakeRealtimeDatabase();

        var result = await database.Ref("nonexistent").GetAsync<TestData>();

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Exists);
    }

    [Fact]
    public async Task Database_PushAsync_GeneratesKey()
    {
        var database = new FakeRealtimeDatabase();

        var result = await database.Ref("items").PushAsync(new TestData { Name = "Pushed", Value = 1 });

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Key);
        Assert.StartsWith("-", result.Value.Key);
    }

    [Fact]
    public async Task Database_RemoveAsync_DeletesData()
    {
        var database = new FakeRealtimeDatabase();
        await database.Ref("items/item1").SetAsync(new TestData { Name = "ToDelete" });

        var deleteResult = await database.Ref("items/item1").RemoveAsync();
        var getResult = await database.Ref("items/item1").GetAsync<TestData>();

        Assert.True(deleteResult.IsSuccess);
        Assert.False(getResult.Value.Exists);
    }

    [Fact]
    public async Task Database_OnValue_NotifiesOnChange()
    {
        var database = new FakeRealtimeDatabase();
        await database.Ref("items/item1").SetAsync(new TestData { Name = "Original" });

        var snapshots = new List<DataSnapshot<TestData>>();
        var unsubscribe = database.Ref("items/item1").OnValue<TestData>(s => snapshots.Add(s));

        // Initial snapshot
        Assert.Single(snapshots);
        Assert.Equal("Original", snapshots[0].Value?.Name);

        // Update triggers new snapshot
        await database.Ref("items/item1").SetAsync(new TestData { Name = "Updated" });
        Assert.Equal(2, snapshots.Count);
        Assert.Equal("Updated", snapshots[1].Value?.Name);

        unsubscribe();
    }

    [Fact]
    public void Database_Child_NavigatesCorrectly()
    {
        var database = new FakeRealtimeDatabase();
        var parent = database.Ref("root");
        var child = parent.Child("subfolder").Child("item");

        Assert.Equal("root/subfolder/item", child.Path);
        Assert.Equal("item", child.Key);
    }

    [Fact]
    public void Database_SimulateError_CausesFailure()
    {
        var database = new FakeRealtimeDatabase();
        database.SimulateError(new FirebaseError("database/permission-denied", "Access denied"));

        var result = database.Ref("items").GetAsync<TestData>().Result;

        Assert.True(result.IsFailure);
        Assert.Equal("database/permission-denied", result.Error?.Code);
    }

    [Fact]
    public void Database_SeedData_PrePopulates()
    {
        var database = new FakeRealtimeDatabase();
        database.SeedData("items/item1", new TestData { Name = "Seeded", Value = 100 });

        var result = database.Ref("items/item1").GetAsync<TestData>().Result;

        Assert.True(result.Value.Exists);
        Assert.Equal("Seeded", result.Value.Value?.Name);
    }
}

/// <summary>
/// Tests for FakeAppCheck test double.
/// </summary>
public class FakeAppCheckTests
{
    [Fact]
    public void FakeAppCheck_InitializesWithNoToken()
    {
        var appCheck = new FakeAppCheck();

        Assert.Null(appCheck.CurrentToken);
    }

    [Fact]
    public async Task FakeAppCheck_ActivateAsync_Succeeds()
    {
        var appCheck = new FakeAppCheck();

        var result = await appCheck.ActivateAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task FakeAppCheck_GetTokenAsync_ReturnsToken()
    {
        var appCheck = new FakeAppCheck();

        var result = await appCheck.GetTokenAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Token);
        Assert.False(result.Value.IsExpired);
    }

    [Fact]
    public async Task FakeAppCheck_GetTokenAsync_CachesToken()
    {
        var appCheck = new FakeAppCheck();

        var result1 = await appCheck.GetTokenAsync();
        var result2 = await appCheck.GetTokenAsync();

        Assert.Equal(result1.Value.Token, result2.Value.Token);
    }

    [Fact]
    public async Task FakeAppCheck_GetTokenAsync_ForceRefresh_GeneratesNewToken()
    {
        var appCheck = new FakeAppCheck();

        var result1 = await appCheck.GetTokenAsync();
        var result2 = await appCheck.GetTokenAsync(forceRefresh: true);

        Assert.NotEqual(result1.Value.Token, result2.Value.Token);
    }

    [Fact]
    public async Task FakeAppCheck_OnTokenChanged_RaisedOnRefresh()
    {
        var appCheck = new FakeAppCheck();
        AppCheckToken? receivedToken = null;
        appCheck.OnTokenChanged += token => receivedToken = token;

        await appCheck.GetTokenAsync(forceRefresh: true);

        Assert.NotNull(receivedToken);
    }

    [Fact]
    public void FakeAppCheck_ConfigureToken_SetsSpecificToken()
    {
        var appCheck = new FakeAppCheck();
        var customToken = new AppCheckToken
        {
            Token = "custom-test-token",
            ExpireTimeMillis = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds()
        };
        appCheck.ConfigureToken(customToken);

        var result = appCheck.GetTokenAsync().Result;

        Assert.Equal("custom-test-token", result.Value.Token);
    }

    [Fact]
    public void FakeAppCheck_SimulateError_CausesFailure()
    {
        var appCheck = new FakeAppCheck();
        appCheck.SimulateError(new FirebaseError("appcheck/token-expired", "Token expired"));

        var result = appCheck.GetTokenAsync().Result;

        Assert.True(result.IsFailure);
        Assert.Equal("appcheck/token-expired", result.Error?.Code);
    }
}
