using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

/// <summary>
/// Tests for FakeFirestore test double.
/// </summary>
public class FakeFirestoreTests
{
    public class TestDocument
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public async Task Collection_AddAsync_StoresDocument()
    {
        var firestore = new FakeFirestore();

        var result = await firestore.Collection<TestDocument>("users").AddAsync(new TestDocument { Name = "John", Age = 30 });

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Id);
        Assert.StartsWith("users/", result.Value.Path);
    }

    [Fact]
    public async Task Document_SetAsync_StoresData()
    {
        var firestore = new FakeFirestore();
        var doc = firestore.Collection<TestDocument>("users").Document("user-1");

        var result = await doc.SetAsync(new TestDocument { Name = "Jane", Age = 25 });

        Assert.True(result.IsSuccess);
        Assert.Equal("user-1", doc.Id);
    }

    [Fact]
    public async Task Document_GetAsync_RetrievesData()
    {
        var firestore = new FakeFirestore();
        var doc = firestore.Collection<TestDocument>("users").Document("user-1");
        await doc.SetAsync(new TestDocument { Name = "Jane", Age = 25 });

        var result = await doc.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Exists);
        Assert.Equal("Jane", result.Value.Data?.Name);
        Assert.Equal(25, result.Value.Data?.Age);
    }

    [Fact]
    public async Task Document_GetAsync_ReturnsNotExists_WhenMissing()
    {
        var firestore = new FakeFirestore();
        var doc = firestore.Collection<TestDocument>("users").Document("nonexistent");

        var result = await doc.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Exists);
        Assert.Null(result.Value.Data);
    }

    [Fact]
    public async Task Document_DeleteAsync_RemovesDocument()
    {
        var firestore = new FakeFirestore();
        var doc = firestore.Collection<TestDocument>("users").Document("user-1");
        await doc.SetAsync(new TestDocument { Name = "Jane", Age = 25 });

        var deleteResult = await doc.DeleteAsync();
        var getResult = await doc.GetAsync();

        Assert.True(deleteResult.IsSuccess);
        Assert.False(getResult.Value.Exists);
    }

    [Fact]
    public async Task Collection_GetAsync_ReturnsAllDocuments()
    {
        var firestore = new FakeFirestore();
        var collection = firestore.Collection<TestDocument>("users");
        await collection.Document("user-1").SetAsync(new TestDocument { Name = "Alice", Age = 20 });
        await collection.Document("user-2").SetAsync(new TestDocument { Name = "Bob", Age = 30 });

        var result = await collection.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Collection_Take_LimitsResults()
    {
        var firestore = new FakeFirestore();
        var collection = firestore.Collection<TestDocument>("users");
        for (int i = 0; i < 5; i++)
        {
            await collection.Document($"user-{i}").SetAsync(new TestDocument { Name = $"User{i}", Age = i * 10 });
        }

        var result = await collection.Take(2).GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Document_OnSnapshot_NotifiesOnChange()
    {
        var firestore = new FakeFirestore();
        var doc = firestore.Collection<TestDocument>("users").Document("user-1");
        await doc.SetAsync(new TestDocument { Name = "Original", Age = 20 });

        var snapshots = new List<DocumentSnapshot<TestDocument>?>();
        var unsubscribe = doc.OnSnapshot(snapshot => snapshots.Add(snapshot));

        // Initial snapshot
        Assert.Single(snapshots);
        Assert.Equal("Original", snapshots[0]?.Data?.Name);

        // Update triggers new snapshot
        await doc.SetAsync(new TestDocument { Name = "Updated", Age = 25 });
        Assert.Equal(2, snapshots.Count);
        Assert.Equal("Updated", snapshots[1]?.Data?.Name);

        // Unsubscribe stops notifications
        unsubscribe();
        await doc.SetAsync(new TestDocument { Name = "Final", Age = 30 });
        Assert.Equal(2, snapshots.Count);
    }

    [Fact]
    public async Task SubCollection_WorksCorrectly()
    {
        var firestore = new FakeFirestore();
        var userDoc = firestore.Collection<TestDocument>("users").Document("user-1");
        await userDoc.SetAsync(new TestDocument { Name = "Parent" });

        var postCollection = userDoc.Collection<TestDocument>("posts");
        await postCollection.Document("post-1").SetAsync(new TestDocument { Name = "First Post" });

        var result = await postCollection.Document("post-1").GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("First Post", result.Value.Data?.Name);
        Assert.Equal("users/user-1/posts/post-1", result.Value.Path);
    }

    [Fact]
    public void SeedData_PrePopulatesCollection()
    {
        var firestore = new FakeFirestore();
        firestore.SeedData("users", new Dictionary<string, TestDocument>
        {
            ["user-1"] = new TestDocument { Name = "Seeded User 1", Age = 20 },
            ["user-2"] = new TestDocument { Name = "Seeded User 2", Age = 30 }
        });

        var result = firestore.Collection<TestDocument>("users").GetAsync().Result;

        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public void SimulateError_CausesNextOperationToFail()
    {
        var firestore = new FakeFirestore();
        firestore.SimulateError(new FirebaseError("firestore/permission-denied", "Access denied"));

        var result = firestore.Collection<TestDocument>("users").GetAsync().Result;

        Assert.True(result.IsFailure);
        Assert.Equal("firestore/permission-denied", result.Error?.Code);
    }
}
