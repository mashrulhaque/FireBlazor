using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

/// <summary>
/// Tests for FakeFirebaseAuth test double.
/// </summary>
public class FakeFirebaseAuthTests
{
    [Fact]
    public void FakeAuth_InitializesWithNoUser()
    {
        var auth = new FakeFirebaseAuth();

        Assert.Null(auth.CurrentUser);
        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public async Task SignInWithEmailAsync_Success_WithConfiguredUser()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password123", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com",
            DisplayName = "Test User"
        });

        var result = await auth.SignInWithEmailAsync("test@example.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal("user-123", result.Value.Uid);
        Assert.Equal("test@example.com", auth.CurrentUser?.Email);
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task SignInWithEmailAsync_Failure_InvalidCredentials()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "correctPassword");

        var result = await auth.SignInWithEmailAsync("test@example.com", "wrongPassword");

        Assert.True(result.IsFailure);
        Assert.Equal("auth/wrong-password", result.Error?.Code);
        Assert.Null(auth.CurrentUser);
    }

    [Fact]
    public async Task SignInWithEmailAsync_Failure_UserNotFound()
    {
        var auth = new FakeFirebaseAuth();

        var result = await auth.SignInWithEmailAsync("nonexistent@example.com", "password");

        Assert.True(result.IsFailure);
        Assert.Equal("auth/user-not-found", result.Error?.Code);
    }

    [Fact]
    public async Task CreateUserWithEmailAsync_Success()
    {
        var auth = new FakeFirebaseAuth();

        var result = await auth.CreateUserWithEmailAsync("new@example.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", result.Value.Email);
        Assert.NotEmpty(result.Value.Uid);
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task CreateUserWithEmailAsync_Failure_EmailAlreadyInUse()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("existing@example.com", "password");

        var result = await auth.CreateUserWithEmailAsync("existing@example.com", "newpassword");

        Assert.True(result.IsFailure);
        Assert.Equal("auth/email-already-in-use", result.Error?.Code);
    }

    [Fact]
    public async Task SignOutAsync_ClearsCurrentUser()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password");
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var result = await auth.SignOutAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(auth.CurrentUser);
        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public async Task OnAuthStateChanged_RaisedOnSignIn()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password");
        FirebaseUser? receivedUser = null;
        auth.OnAuthStateChanged += user => receivedUser = user;

        await auth.SignInWithEmailAsync("test@example.com", "password");

        Assert.NotNull(receivedUser);
        Assert.Equal("test@example.com", receivedUser?.Email);
    }

    [Fact]
    public async Task OnAuthStateChanged_RaisedOnSignOut()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password");
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var eventCount = 0;
        auth.OnAuthStateChanged += _ => eventCount++;

        await auth.SignOutAsync();

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_Success()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password");

        var result = await auth.SendPasswordResetEmailAsync("test@example.com");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SignInWithGoogleAsync_Success_WhenConfigured()
    {
        var auth = new FakeFirebaseAuth();
        var googleUser = new FirebaseUser
        {
            Uid = "google-123",
            Email = "google@example.com",
            Providers = ["google.com"]
        };
        auth.ConfigureOAuthProvider("google", googleUser);

        var result = await auth.SignInWithGoogleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("google-123", result.Value.Uid);
        Assert.Contains("google.com", result.Value.Providers);
    }

    [Fact]
    public async Task SignInWithGoogleAsync_Failure_WhenNotConfigured()
    {
        var auth = new FakeFirebaseAuth();

        var result = await auth.SignInWithGoogleAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("auth/popup-closed-by-user", result.Error?.Code);
    }

    [Fact]
    public void SimulateError_CausesNextOperationToFail()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password");
        auth.SimulateError(new FirebaseError("auth/network-request-failed", "Network error"));

        var result = auth.SignInWithEmailAsync("test@example.com", "password").Result;

        Assert.True(result.IsFailure);
        Assert.Equal("auth/network-request-failed", result.Error?.Code);
    }

    [Fact]
    public void SetCurrentUser_DirectlyUpdatesState()
    {
        var auth = new FakeFirebaseAuth();
        var user = new FirebaseUser { Uid = "direct-user", Email = "direct@example.com" };

        auth.SetCurrentUser(user);

        Assert.Equal("direct-user", auth.CurrentUser?.Uid);
        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public async Task GetIdTokenAsync_Success_ReturnsToken()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password123", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        await auth.SignInWithEmailAsync("test@example.com", "password123");

        var result = await auth.GetIdTokenAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.Contains(".", result.Value); // JWT format
    }

    [Fact]
    public async Task GetIdTokenAsync_NoUser_ReturnsError()
    {
        var auth = new FakeFirebaseAuth();

        var result = await auth.GetIdTokenAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("auth/no-user", result.Error?.Code);
    }

    [Fact]
    public async Task GetIdTokenAsync_CustomGenerator_UsesGenerator()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password123", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        auth.ConfigureTokenGenerator(user => $"custom-token-for-{user.Uid}");
        await auth.SignInWithEmailAsync("test@example.com", "password123");

        var result = await auth.GetIdTokenAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("custom-token-for-user-123", result.Value);
    }
}
