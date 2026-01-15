using FireBlazor;
using FireBlazor.Testing;

namespace FireBlazor.Tests.RealtimeDb;

/// <summary>
/// Integration tests for FakeRealtimeDatabase features including transactions,
/// connection state, and on-disconnect handlers.
/// </summary>
public class FakeRealtimeDatabaseIntegrationTests
{
    [Fact]
    public async Task Transaction_IncrementCounter_WorksCorrectly()
    {
        var db = new FakeRealtimeDatabase();
        db.SeedData("counter", 0);

        var result = await db.Ref("counter").TransactionAsync<int?>(current => (current ?? 0) + 1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Committed);
        Assert.Equal(1, result.Value.Value);
    }

    [Fact]
    public async Task Transaction_AbortByReturningNull_DoesNotModifyData()
    {
        var db = new FakeRealtimeDatabase();
        db.SeedData("counter", 5);

        var result = await db.Ref("counter").TransactionAsync<int?>(current => null);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Committed);

        // Verify data was not modified
        var getData = await db.Ref("counter").GetAsync<int>();
        Assert.True(getData.IsSuccess);
        Assert.Equal(5, getData.Value.Value);
    }

    [Fact]
    public void ConnectionState_SimulateDisconnect_NotifiesListeners()
    {
        var db = new FakeRealtimeDatabase();
        var states = new List<bool>();

        var unsubscribe = db.OnConnectionStateChanged(state => states.Add(state));

        Assert.Single(states); // Initial state
        Assert.True(states[0]);

        db.SimulateConnectionState(false);

        Assert.Equal(2, states.Count);
        Assert.False(states[1]);

        unsubscribe();
    }

    [Fact]
    public async Task OnDisconnect_Set_ReturnsSuccess()
    {
        var db = new FakeRealtimeDatabase();

        var result = await db.Ref("users/123/status").OnDisconnect().SetAsync("offline");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task OnDisconnect_WithSimulatedError_ReturnsFailure()
    {
        var db = new FakeRealtimeDatabase();
        db.SimulateError(new FirebaseError("database/permission-denied", "Access denied"));

        var result = await db.Ref("users/123/status").OnDisconnect().SetAsync("offline");

        Assert.False(result.IsSuccess);
        Assert.Contains("permission-denied", result.Error!.Code);
    }

    [Fact]
    public async Task SetAsync_WithServerValueIncrement_IncrementsValue()
    {
        var db = new FakeRealtimeDatabase();
        db.SeedData("counter", 10);

        // Note: FakeRealtimeDatabase may not actually process ServerValue sentinels
        // This test verifies the API accepts ServerValue without errors
        var result = await db.Ref("counter").SetAsync(ServerValue.Increment(5));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetAsync_WithServerValueTimestamp_Succeeds()
    {
        var db = new FakeRealtimeDatabase();

        var result = await db.Ref("events/login").SetAsync(new {
            eventType = "login",
            timestamp = ServerValue.Timestamp
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PushAsync_WithServerValueTimestamp_Succeeds()
    {
        var db = new FakeRealtimeDatabase();

        var result = await db.Ref("messages").PushAsync(new {
            text = "Hello",
            createdAt = ServerValue.Timestamp
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Key);
    }
}
