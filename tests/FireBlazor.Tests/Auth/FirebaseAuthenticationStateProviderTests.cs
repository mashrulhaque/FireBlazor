using System.Security.Claims;
using FireBlazor.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FireBlazor.Tests.Auth;

public class FirebaseAuthenticationStateProviderTests
{
    private static ILogger<FirebaseAuthenticationStateProvider> NullLogger =>
        new NullLogger<FirebaseAuthenticationStateProvider>();

    [Fact]
    public async Task GetAuthenticationStateAsync_NoUser_ReturnsUnauthenticated()
    {
        var auth = new FakeFirebaseAuth();
        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WithUser_ReturnsAuthenticated()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com",
            DisplayName = "Test User"
        });
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("user-123", state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("test@example.com", state.User.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("Test User", state.User.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WithRoles_ExtractsRoles()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("admin@example.com", "password", new FirebaseUser
        {
            Uid = "admin-123",
            Email = "admin@example.com"
        });
        auth.ConfigureTokenGenerator(user => CreateTokenWithRoles(user, ["admin", "editor"]));
        await auth.SignInWithEmailAsync("admin@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.IsInRole("admin"));
        Assert.True(state.User.IsInRole("editor"));
        Assert.False(state.User.IsInRole("superuser"));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_CustomRolesClaimName_UsesConfiguredName()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        auth.ConfigureTokenGenerator(user => CreateTokenWithCustomClaim(user, "permissions", ["read", "write"]));
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions { RolesClaimName = "permissions" };
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.IsInRole("read"));
        Assert.True(state.User.IsInRole("write"));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_SingleStringRole_ExtractsRole()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        auth.ConfigureTokenGenerator(user => CreateTokenWithSingleRole(user, "viewer"));
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.IsInRole("viewer"));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_MalformedToken_ReturnsNoRoles()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        // Token with only 2 parts instead of 3
        auth.ConfigureTokenGenerator(_ => "header.payload");
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Empty(state.User.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_InvalidBase64Token_ReturnsNoRoles()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        // Invalid base64 in payload
        auth.ConfigureTokenGenerator(_ => "header.!!!invalid-base64!!!.signature");
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Empty(state.User.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_InvalidJsonInToken_ReturnsNoRoles()
    {
        var auth = new FakeFirebaseAuth();
        auth.AddUser("test@example.com", "password", new FirebaseUser
        {
            Uid = "user-123",
            Email = "test@example.com"
        });
        // Valid base64 but invalid JSON
        var invalidJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("not valid json"));
        auth.ConfigureTokenGenerator(_ => $"header.{invalidJson}.signature");
        await auth.SignInWithEmailAsync("test@example.com", "password");

        var options = new FirebaseAuthorizationOptions();
        var provider = new FirebaseAuthenticationStateProvider(auth, options, NullLogger);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Empty(state.User.FindAll(ClaimTypes.Role));
    }

    private static string CreateTokenWithRoles(FirebaseUser user, string[] roles)
    {
        return CreateTokenWithCustomClaim(user, "roles", roles);
    }

    private static string CreateTokenWithSingleRole(FirebaseUser user, string role)
    {
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        // Add exp claim set to 1 hour in the future
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadObj = new Dictionary<string, object>
        {
            ["sub"] = user.Uid,
            ["email"] = user.Email ?? "",
            ["roles"] = role, // Single string, not array
            ["exp"] = exp
        };
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(payloadObj)));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake-signature"));
        return $"{header}.{payload}.{signature}";
    }

    private static string CreateTokenWithCustomClaim(FirebaseUser user, string claimName, string[] values)
    {
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        // Add exp claim set to 1 hour in the future
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payloadObj = new Dictionary<string, object>
        {
            ["sub"] = user.Uid,
            ["email"] = user.Email ?? "",
            [claimName] = values,
            ["exp"] = exp
        };
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(payloadObj)));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake-signature"));
        return $"{header}.{payload}.{signature}";
    }
}
