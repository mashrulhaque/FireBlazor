using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

/// <summary>
/// Tests for FakeFirebase gateway test double.
/// </summary>
public class FakeFirebaseTests
{
    [Fact]
    public void FakeFirebase_ProvidesAllServices()
    {
        var firebase = new FakeFirebase();

        Assert.NotNull(firebase.Auth);
        Assert.NotNull(firebase.Firestore);
        Assert.NotNull(firebase.Storage);
        Assert.NotNull(firebase.RealtimeDb);
        Assert.NotNull(firebase.AppCheck);
    }

    [Fact]
    public void FakeFirebase_ServicesAreFakes()
    {
        var firebase = new FakeFirebase();

        Assert.IsType<FakeFirebaseAuth>(firebase.Auth);
        Assert.IsType<FakeFirestore>(firebase.Firestore);
        Assert.IsType<FakeFirebaseStorage>(firebase.Storage);
        Assert.IsType<FakeRealtimeDatabase>(firebase.RealtimeDb);
        Assert.IsType<FakeAppCheck>(firebase.AppCheck);
    }

    [Fact]
    public void FakeFirebase_ProvidesFakeAccessors()
    {
        var firebase = new FakeFirebase();

        // Should be able to access typed fakes for configuration
        Assert.NotNull(firebase.FakeAuth);
        Assert.NotNull(firebase.FakeFirestore);
        Assert.NotNull(firebase.FakeStorage);
        Assert.NotNull(firebase.FakeRealtimeDb);
        Assert.NotNull(firebase.FakeAppCheck);
    }

    [Fact]
    public void FakeFirebase_OnOperationEvent_Works()
    {
        var firebase = new FakeFirebase();
        FirebaseOperation? receivedOperation = null;
        firebase.OnOperation += op => receivedOperation = op;

        firebase.RaiseOperation(new FirebaseOperation("Auth", "SignIn", TimeSpan.FromMilliseconds(100), true));

        Assert.NotNull(receivedOperation);
        Assert.Equal("Auth", receivedOperation?.Service);
    }

    [Fact]
    public void FakeFirebase_OnErrorEvent_Works()
    {
        var firebase = new FakeFirebase();
        FirebaseException? receivedException = null;
        firebase.OnError += ex => receivedException = ex;

        firebase.RaiseError(new FirebaseException("test/error", "Test error message"));

        Assert.NotNull(receivedException);
        Assert.Equal("test/error", receivedException?.Code);
    }

    [Fact]
    public async Task FakeFirebase_IntegrationTest_AuthAndFirestore()
    {
        var firebase = new FakeFirebase();

        // Configure auth
        firebase.FakeAuth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });

        // Sign in
        var authResult = await firebase.Auth.SignInWithEmailAsync("test@example.com", "password");
        Assert.True(authResult.IsSuccess);

        // Use Firestore
        var doc = firebase.Firestore.Collection<TestDoc>("users").Document("user-123");
        await doc.SetAsync(new TestDoc { Name = "Test User" });

        var getResult = await doc.GetAsync();
        Assert.True(getResult.IsSuccess);
        Assert.Equal("Test User", getResult.Value.Data?.Name);
    }

    [Fact]
    public void FakeFirebase_Reset_ClearsAllState()
    {
        var firebase = new FakeFirebase();

        // Add some state
        firebase.FakeAuth.AddUser("test@example.com", "password");
        firebase.FakeAuth.SignInWithEmailAsync("test@example.com", "password").Wait();

        // Reset
        firebase.Reset();

        // State should be cleared
        Assert.Null(firebase.Auth.CurrentUser);
        Assert.False(firebase.Auth.IsAuthenticated);
    }

    private class TestDoc
    {
        public string? Name { get; set; }
    }
}
