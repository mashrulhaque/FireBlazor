namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IFirebaseAuth using JavaScript interop.
/// </summary>
internal sealed class WasmFirebaseAuth : IFirebaseAuth
{
    private readonly FirebaseJsInterop _jsInterop;
    private FirebaseUser? _currentUser;

    public FirebaseUser? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public event Action<FirebaseUser?>? OnAuthStateChanged;

    public WasmFirebaseAuth(FirebaseJsInterop jsInterop)
    {
        _jsInterop = jsInterop;
    }

    public async Task<Result<FirebaseUser>> SignInWithEmailAsync(string email, string password)
    {
        var result = await _jsInterop.SignInWithEmailAsync(email, password);
        return ProcessAuthResult(result);
    }

    public async Task<Result<FirebaseUser>> CreateUserWithEmailAsync(string email, string password)
    {
        var result = await _jsInterop.CreateUserWithEmailAsync(email, password);
        return ProcessAuthResult(result);
    }

    public async Task<Result<Unit>> SendPasswordResetEmailAsync(string email)
    {
        var result = await _jsInterop.SendPasswordResetEmailAsync(email);
        if (!result.Success)
            return Result<Unit>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        return Unit.Value;
    }

    public async Task<Result<FirebaseUser>> SignInWithGoogleAsync()
    {
        var result = await _jsInterop.SignInWithGoogleAsync();
        return ProcessAuthResult(result);
    }

    public async Task<Result<FirebaseUser>> SignInWithGitHubAsync()
    {
        var result = await _jsInterop.SignInWithGitHubAsync();
        return ProcessAuthResult(result);
    }

    public async Task<Result<FirebaseUser>> SignInWithMicrosoftAsync()
    {
        var result = await _jsInterop.SignInWithMicrosoftAsync();
        return ProcessAuthResult(result);
    }

    public Task<Result<FirebaseUser>> SignInWithAppleAsync()
    {
        throw new NotImplementedException("SignInWithAppleAsync is not yet supported in WebAssembly.");
    }

    public Task<Result<FirebaseUser>> SignInWithFacebookAsync()
    {
        throw new NotImplementedException("SignInWithFacebookAsync is not yet supported in WebAssembly.");
    }

    public async Task<Result<Unit>> SignOutAsync()
    {
        var result = await _jsInterop.SignOutAsync();
        if (!result.Success)
            return Result<Unit>.Failure(new FirebaseError(result.Error!.Code, result.Error.Message));

        _currentUser = null;
        OnAuthStateChanged?.Invoke(null);
        return Unit.Value;
    }

    public async Task<Result<string>> GetIdTokenAsync(bool forceRefresh = false)
    {
        var result = await _jsInterop.GetIdTokenAsync(forceRefresh);
        if (!result.Success)
            return Result<string>.Failure(new FirebaseError(result.Error?.Code ?? "auth/token-error", result.Error?.Message ?? "Failed to get ID token"));

        return result.Data ?? "";
    }

    private Result<FirebaseUser> ProcessAuthResult(JsResult<JsUser> result)
    {
        if (!result.Success)
            return Result<FirebaseUser>.Failure(new FirebaseError(result.Error?.Code ?? "auth/unknown", result.Error?.Message ?? "Unknown authentication error"));

        if (result.Data is null)
            return Result<FirebaseUser>.Failure(new FirebaseError("auth/unknown", "No user data returned from authentication"));

        _currentUser = MapUser(result.Data);
        OnAuthStateChanged?.Invoke(_currentUser);
        return _currentUser;
    }

    private static FirebaseUser MapUser(JsUser jsUser) => new()
    {
        Uid = jsUser.Uid,
        Email = jsUser.Email,
        DisplayName = jsUser.DisplayName,
        PhotoUrl = jsUser.PhotoUrl,
        IsEmailVerified = jsUser.IsEmailVerified,
        IsAnonymous = jsUser.IsAnonymous,
        Providers = jsUser.Providers,
        CreatedAt = ParseDateTime(jsUser.CreatedAt),
        LastSignInAt = ParseDateTime(jsUser.LastSignInAt)
    };

    private static DateTimeOffset? ParseDateTime(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        return DateTimeOffset.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var result) ? result : null;
    }
}
