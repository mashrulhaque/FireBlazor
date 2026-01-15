using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

public class FakeFirestoreFieldValueTests
{
    private readonly FakeFirestore _firestore = new();

    public class Counter
    {
        public int Count { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    [Fact]
    public async Task UpdateAsync_WithIncrement_IncreasesValue()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Count = 10 });

        // Act
        var result = await doc.UpdateAsync(new { Count = FieldValue.Increment(5) });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.Equal(15, snapshot.Value.Data!.Count);
    }

    [Fact]
    public async Task UpdateAsync_WithServerTimestamp_SetsCurrentTime()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Count = 0 });
        var before = DateTime.UtcNow;

        // Act
        var result = await doc.UpdateAsync(new { LastUpdated = FieldValue.ServerTimestamp() });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.NotNull(snapshot.Value.Data!.LastUpdated);
        Assert.True(snapshot.Value.Data.LastUpdated >= before);
    }

    [Fact]
    public async Task UpdateAsync_WithArrayUnion_AddsElements()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Tags = new List<string> { "a", "b" } });

        // Act
        var result = await doc.UpdateAsync(new { Tags = FieldValue.ArrayUnion("c", "d") });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.Equal(new[] { "a", "b", "c", "d" }, snapshot.Value.Data!.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithArrayUnion_DoesNotAddDuplicates()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Tags = new List<string> { "a", "b" } });

        // Act
        var result = await doc.UpdateAsync(new { Tags = FieldValue.ArrayUnion("b", "c") });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.Equal(new[] { "a", "b", "c" }, snapshot.Value.Data!.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithArrayRemove_RemovesElements()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Tags = new List<string> { "a", "b", "c" } });

        // Act
        var result = await doc.UpdateAsync(new { Tags = FieldValue.ArrayRemove("b") });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.Equal(new[] { "a", "c" }, snapshot.Value.Data!.Tags);
    }

    [Fact]
    public async Task UpdateAsync_WithDelete_RemovesField()
    {
        // Arrange
        var doc = _firestore.Collection<Counter>("counters").Document("c1");
        await doc.SetAsync(new Counter { Count = 10, LastUpdated = DateTime.UtcNow });

        // Act
        var result = await doc.UpdateAsync(new { LastUpdated = FieldValue.Delete() });

        // Assert
        Assert.True(result.IsSuccess);
        var snapshot = await doc.GetAsync();
        Assert.Null(snapshot.Value.Data!.LastUpdated);
    }
}
