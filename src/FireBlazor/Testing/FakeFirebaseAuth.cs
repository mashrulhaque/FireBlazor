namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IFirebaseAuth for testing.
/// Allows configuring users and simulating auth scenarios.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FakeFirebaseAuth : IFirebaseAuth
{
    private readonly Dictionary<string, (string Password, FirebaseUser User)> _users = new();
    private readonly Dictionary<string, FirebaseUser> _oauthProviders = new();
    private FirebaseUser? _currentUser;
    private FirebaseError? _simulatedError;
    private Func<FirebaseUser, string>? _tokenGenerator;

    public FirebaseUser? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public event Action<FirebaseUser?>? OnAuthStateChanged;

    /// <summary>
    /// Adds a user to the fake auth store.
    /// </summary>
    public void AddUser(string email, string password, FirebaseUser? user = null)
    {
        var firebaseUser = user ?? new FirebaseUser
        {
            Uid = Guid.NewGuid().ToString(),
            Email = email,
            Providers = ["password"]
        };
        _users[email.ToLowerInvariant()] = (password, firebaseUser);
    }

    /// <summary>
    /// Configures an OAuth provider to return a specific user.
    /// </summary>
    public void ConfigureOAuthProvider(string provider, FirebaseUser user)
    {
        _oauthProviders[provider.ToLowerInvariant()] = user;
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Configures a custom token generator for testing.
    /// The generator receives the current user and should return a JWT-like token string.
    /// </summary>
    public void ConfigureTokenGenerator(Func<FirebaseUser, string> generator)
    {
        _tokenGenerator = generator;
    }

    /// <summary>
    /// Directly sets the current user (for testing scenarios).
    /// </summary>
    public void SetCurrentUser(FirebaseUser? user)
    {
        _currentUser = user;
        OnAuthStateChanged?.Invoke(_currentUser);
    }

    public Task<Result<FirebaseUser>> SignInWithEmailAsync(string email, string password)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<FirebaseUser>.Failure(error!));

        var key = email.ToLowerInvariant();
        if (!_users.TryGetValue(key, out var stored))
            return Task.FromResult(Result<FirebaseUser>.Failure(new FirebaseError("auth/user-not-found", "User not found")));

        if (stored.Password != password)
            return Task.FromResult(Result<FirebaseUser>.Failure(new FirebaseError("auth/wrong-password", "Wrong password")));

        _currentUser = stored.User;
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult(Result<FirebaseUser>.Success(stored.User));
    }

    public Task<Result<FirebaseUser>> CreateUserWithEmailAsync(string email, string password)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<FirebaseUser>.Failure(error!));

        var key = email.ToLowerInvariant();
        if (_users.ContainsKey(key))
            return Task.FromResult(Result<FirebaseUser>.Failure(new FirebaseError("auth/email-already-in-use", "Email already in use")));

        var user = new FirebaseUser
        {
            Uid = Guid.NewGuid().ToString(),
            Email = email,
            Providers = ["password"],
            CreatedAt = DateTimeOffset.UtcNow
        };
        _users[key] = (password, user);
        _currentUser = user;
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult(Result<FirebaseUser>.Success(user));
    }

    public Task<Result<Unit>> SendPasswordResetEmailAsync(string email)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        // Just succeed - in tests we don't actually send emails
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<FirebaseUser>> SignInWithGoogleAsync() => SignInWithOAuthAsync("google");
    public Task<Result<FirebaseUser>> SignInWithGitHubAsync() => SignInWithOAuthAsync("github");
    public Task<Result<FirebaseUser>> SignInWithMicrosoftAsync() => SignInWithOAuthAsync("microsoft");
    public Task<Result<FirebaseUser>> SignInWithAppleAsync() => SignInWithOAuthAsync("apple");
    public Task<Result<FirebaseUser>> SignInWithFacebookAsync() => SignInWithOAuthAsync("facebook");

    private Task<Result<FirebaseUser>> SignInWithOAuthAsync(string provider)
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<FirebaseUser>.Failure(error!));

        if (!_oauthProviders.TryGetValue(provider.ToLowerInvariant(), out var user))
            return Task.FromResult(Result<FirebaseUser>.Failure(new FirebaseError("auth/popup-closed-by-user", "OAuth not configured for testing")));

        _currentUser = user;
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult(Result<FirebaseUser>.Success(user));
    }

    public Task<Result<Unit>> SignOutAsync()
    {
        if (TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _currentUser = null;
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<string>> GetIdTokenAsync(bool forceRefresh = false)
    {
        if (_simulatedError != null)
        {
            var error = _simulatedError;
            _simulatedError = null;
            return Task.FromResult(Result<string>.Failure(error));
        }

        if (_currentUser == null)
            return Task.FromResult(Result<string>.Failure(new FirebaseError("auth/no-user", "No user is currently signed in")));

        var token = _tokenGenerator?.Invoke(_currentUser) ?? GenerateDefaultToken(_currentUser);
        return Task.FromResult(Result<string>.Success(token));
    }

    private static string GenerateDefaultToken(FirebaseUser user)
    {
        // Generate a fake JWT-like token for testing
        // Format: header.payload.signature (base64 encoded)
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                sub = user.Uid,
                email = user.Email,
                email_verified = user.IsEmailVerified,
                name = user.DisplayName,
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            })));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake-signature"));
        return $"{header}.{payload}.{signature}";
    }

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        _users.Clear();
        _oauthProviders.Clear();
        _currentUser = null;
        _simulatedError = null;
        _tokenGenerator = null;
    }

    private bool TryConsumeSimulatedError(out FirebaseError? error)
    {
        error = _simulatedError;
        _simulatedError = null;
        return error != null;
    }
}
