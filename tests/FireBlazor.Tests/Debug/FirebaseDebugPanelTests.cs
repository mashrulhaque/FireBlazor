namespace FireBlazor.Tests.Debug;

/// <summary>
/// Tests for FirebaseDebugPanel component.
/// </summary>
public class FirebaseDebugPanelTests
{
    [Fact]
    public void DebugState_InitializesWithDefaultValues()
    {
        var state = new FirebaseDebugState();

        Assert.Null(state.CurrentUser);
        Assert.False(state.IsAuthenticated);
        Assert.Empty(state.RecentOperations);
        Assert.Empty(state.RecentErrors);
        Assert.Equal(0, state.ActiveSubscriptionCount);
    }

    [Fact]
    public void DebugState_TracksOperations()
    {
        var state = new FirebaseDebugState();
        var operation = new FirebaseOperation("Auth", "SignIn", TimeSpan.FromMilliseconds(150), true);

        state.AddOperation(operation);

        Assert.Single(state.RecentOperations);
        Assert.Equal("Auth", state.RecentOperations[0].Service);
    }

    [Fact]
    public void DebugState_TracksErrors()
    {
        var state = new FirebaseDebugState();
        var error = new FirebaseException("auth/invalid-email", "Invalid email format");

        state.AddError(error);

        Assert.Single(state.RecentErrors);
        Assert.Equal("auth/invalid-email", state.RecentErrors[0].Code);
    }

    [Fact]
    public void DebugState_LimitsRecentOperations()
    {
        var state = new FirebaseDebugState { MaxOperations = 5 };

        for (int i = 0; i < 10; i++)
        {
            state.AddOperation(new FirebaseOperation("Test", $"Op{i}", TimeSpan.Zero, true));
        }

        Assert.Equal(5, state.RecentOperations.Count);
        Assert.Equal("Op9", state.RecentOperations[0].Action); // Most recent first
    }

    [Fact]
    public void DebugState_LimitsRecentErrors()
    {
        var state = new FirebaseDebugState { MaxErrors = 3 };

        for (int i = 0; i < 5; i++)
        {
            state.AddError(new FirebaseException($"error/{i}", $"Error {i}"));
        }

        Assert.Equal(3, state.RecentErrors.Count);
        Assert.Equal("error/4", state.RecentErrors[0].Code); // Most recent first
    }

    [Fact]
    public void DebugState_UpdatesAuthState()
    {
        var state = new FirebaseDebugState();
        var user = new FirebaseUser { Uid = "test-uid", Email = "test@example.com" };

        state.UpdateAuthState(user);

        Assert.Equal("test-uid", state.CurrentUser?.Uid);
        Assert.True(state.IsAuthenticated);
    }

    [Fact]
    public void DebugState_ClearsAuthState()
    {
        var state = new FirebaseDebugState();
        var user = new FirebaseUser { Uid = "test-uid" };

        state.UpdateAuthState(user);
        state.UpdateAuthState(null);

        Assert.Null(state.CurrentUser);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public void DebugState_TracksSubscriptionCount()
    {
        var state = new FirebaseDebugState();

        state.UpdateSubscriptionCount(5);

        Assert.Equal(5, state.ActiveSubscriptionCount);
    }

    [Fact]
    public void DebugState_ClearsAllData()
    {
        var state = new FirebaseDebugState();
        state.UpdateAuthState(new FirebaseUser { Uid = "test" });
        state.AddOperation(new FirebaseOperation("Test", "Op", TimeSpan.Zero, true));
        state.AddError(new FirebaseException("test", "error"));
        state.UpdateSubscriptionCount(3);

        state.Clear();

        Assert.Empty(state.RecentOperations);
        Assert.Empty(state.RecentErrors);
        // Note: Auth state is not cleared as it reflects current Firebase state
    }

    // Event firing tests
    [Fact]
    public void DebugState_RaisesStateChanged_OnAddOperation()
    {
        var state = new FirebaseDebugState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        state.AddOperation(new FirebaseOperation("Test", "Op", TimeSpan.Zero, true));

        Assert.True(eventRaised);
    }

    [Fact]
    public void DebugState_RaisesStateChanged_OnAddError()
    {
        var state = new FirebaseDebugState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        state.AddError(new FirebaseException("test", "error"));

        Assert.True(eventRaised);
    }

    [Fact]
    public void DebugState_RaisesStateChanged_OnUpdateAuthState()
    {
        var state = new FirebaseDebugState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        state.UpdateAuthState(new FirebaseUser { Uid = "test" });

        Assert.True(eventRaised);
    }

    [Fact]
    public void DebugState_RaisesStateChanged_OnUpdateSubscriptionCount()
    {
        var state = new FirebaseDebugState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        state.UpdateSubscriptionCount(5);

        Assert.True(eventRaised);
    }

    [Fact]
    public void DebugState_RaisesStateChanged_OnClear()
    {
        var state = new FirebaseDebugState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        state.Clear();

        Assert.True(eventRaised);
    }

    // Thread-safety tests
    [Fact]
    public async Task DebugState_ThreadSafe_ConcurrentOperationAdds()
    {
        var state = new FirebaseDebugState { MaxOperations = 100 };
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => state.AddOperation(new FirebaseOperation("Test", $"Op{i}", TimeSpan.Zero, true)))
        );

        await Task.WhenAll(tasks);

        Assert.Equal(100, state.RecentOperations.Count);
    }

    [Fact]
    public async Task DebugState_ThreadSafe_ConcurrentErrorAdds()
    {
        var state = new FirebaseDebugState { MaxErrors = 50 };
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => state.AddError(new FirebaseException($"error/{i}", $"Error {i}")))
        );

        await Task.WhenAll(tasks);

        Assert.Equal(50, state.RecentErrors.Count);
    }

    [Fact]
    public async Task DebugState_ThreadSafe_ConcurrentAuthUpdates()
    {
        var state = new FirebaseDebugState();
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => state.UpdateAuthState(new FirebaseUser { Uid = $"user-{i}" }))
        );

        await Task.WhenAll(tasks);

        // Should have a valid user set (last write wins, but should not crash)
        Assert.NotNull(state.CurrentUser);
        Assert.True(state.IsAuthenticated);
    }

    [Fact]
    public async Task DebugState_ThreadSafe_ConcurrentMixedOperations()
    {
        var state = new FirebaseDebugState { MaxOperations = 100, MaxErrors = 50 };
        var random = new Random(42);

        var tasks = Enumerable.Range(0, 200).Select(i =>
            Task.Run(() =>
            {
                switch (i % 4)
                {
                    case 0:
                        state.AddOperation(new FirebaseOperation("Test", $"Op{i}", TimeSpan.Zero, true));
                        break;
                    case 1:
                        state.AddError(new FirebaseException($"error/{i}", $"Error {i}"));
                        break;
                    case 2:
                        state.UpdateAuthState(new FirebaseUser { Uid = $"user-{i}" });
                        break;
                    case 3:
                        state.UpdateSubscriptionCount(i);
                        break;
                }
            })
        );

        await Task.WhenAll(tasks);

        // Should not crash and should have valid state
        Assert.NotNull(state.RecentOperations);
        Assert.NotNull(state.RecentErrors);
        Assert.True(state.RecentOperations.Count <= 100);
        Assert.True(state.RecentErrors.Count <= 50);
    }

    // Error handling tests
    [Fact]
    public void DebugState_HandlesExceptionInEventHandler()
    {
        var state = new FirebaseDebugState();
        state.OnStateChanged += () => throw new InvalidOperationException("Test exception");

        // Should not throw
        var exception = Record.Exception(() => state.AddOperation(new FirebaseOperation("Test", "Op", TimeSpan.Zero, true)));

        Assert.Null(exception);
        Assert.Single(state.RecentOperations);
    }
}
