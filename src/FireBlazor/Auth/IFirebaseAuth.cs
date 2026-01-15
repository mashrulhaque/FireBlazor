namespace FireBlazor;

/// <summary>Firebase Authentication service interface.</summary>
public interface IFirebaseAuth
{
    /// <summary>The currently signed-in user, or null if not authenticated.</summary>
    FirebaseUser? CurrentUser { get; }

    /// <summary>Whether a user is currently authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Event raised when auth state changes.</summary>
    event Action<FirebaseUser?>? OnAuthStateChanged;

    // Email/Password
    Task<Result<FirebaseUser>> SignInWithEmailAsync(string email, string password);
    Task<Result<FirebaseUser>> CreateUserWithEmailAsync(string email, string password);
    Task<Result<Unit>> SendPasswordResetEmailAsync(string email);

    // OAuth Providers
    Task<Result<FirebaseUser>> SignInWithGoogleAsync();
    Task<Result<FirebaseUser>> SignInWithGitHubAsync();
    Task<Result<FirebaseUser>> SignInWithMicrosoftAsync();
    Task<Result<FirebaseUser>> SignInWithAppleAsync();
    Task<Result<FirebaseUser>> SignInWithFacebookAsync();

    // Sign Out
    Task<Result<Unit>> SignOutAsync();

    // ID Token
    Task<Result<string>> GetIdTokenAsync(bool forceRefresh = false);
}

/// <summary>Represents an authenticated Firebase user.</summary>
public sealed class FirebaseUser
{
    public required string Uid { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? PhotoUrl { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsAnonymous { get; init; }
    public IReadOnlyList<string> Providers { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? LastSignInAt { get; init; }
}

/// <summary>Represents a void/unit result.</summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
