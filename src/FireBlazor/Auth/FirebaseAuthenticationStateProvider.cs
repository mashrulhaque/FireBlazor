using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace FireBlazor;

/// <summary>
/// Blazor AuthenticationStateProvider that integrates with Firebase Authentication.
/// Provides real-time auth state updates and role-based authorization via Firebase Custom Claims.
/// </summary>
/// <remarks>
/// This provider is designed for Blazor WebAssembly's single-threaded context.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FirebaseAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IFirebaseAuth _auth;
    private readonly FirebaseAuthorizationOptions _options;
    private readonly ILogger<FirebaseAuthenticationStateProvider> _logger;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private bool _initialized;

    public FirebaseAuthenticationStateProvider(
        IFirebaseAuth auth,
        FirebaseAuthorizationOptions options,
        ILogger<FirebaseAuthenticationStateProvider> logger)
    {
        _auth = auth;
        _options = options;
        _logger = logger;
        _auth.OnAuthStateChanged += HandleAuthStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized && _auth.CurrentUser != null)
        {
            await RefreshAuthStateAsync();
        }
        return new AuthenticationState(_currentUser);
    }

    private void HandleAuthStateChanged(FirebaseUser? user)
    {
        _ = HandleAuthStateChangedAsync();
    }

    private async Task HandleAuthStateChangedAsync()
    {
        try
        {
            await RefreshAuthStateAsync();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        catch (Exception ex)
        {
            // Log error but don't crash - auth state change failures shouldn't crash the app
            _logger.LogError(ex, "Error refreshing auth state");
            // Reset to unauthenticated state on error
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
    }

    private async Task RefreshAuthStateAsync()
    {
        _initialized = true;

        if (_auth.CurrentUser == null)
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            return;
        }

        var tokenResult = await _auth.GetIdTokenAsync();
        if (tokenResult.IsFailure)
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            return;
        }

        _currentUser = BuildClaimsPrincipal(_auth.CurrentUser, tokenResult.Value);
    }

    private ClaimsPrincipal BuildClaimsPrincipal(FirebaseUser user, string idToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Uid),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? user.Uid),
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        // Extract roles from ID token
        var roles = ExtractRolesFromToken(idToken);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Firebase");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Extracts role claims from a Firebase ID token (JWT).
    /// </summary>
    /// <param name="idToken">The Firebase ID token in JWT format.</param>
    /// <returns>A collection of role strings extracted from the token, or empty if the token is invalid or expired.</returns>
    /// <remarks>
    /// <para>
    /// <strong>SECURITY NOTE:</strong> This method performs client-side JWT parsing only.
    /// The JWT signature is NOT validated because Firebase's public keys would need to be
    /// fetched and verified, which is not practical in a Blazor WebAssembly context.
    /// </para>
    /// <para>
    /// Full JWT validation (including signature verification) should be performed server-side
    /// when the token is used for API calls. This client-side extraction is only for UI
    /// authorization purposes (showing/hiding UI elements based on roles).
    /// </para>
    /// <para>
    /// This method DOES validate the token expiration (<c>exp</c> claim) to prevent using
    /// stale tokens for UI authorization decisions.
    /// </para>
    /// </remarks>
    private IEnumerable<string> ExtractRolesFromToken(string idToken)
    {
        try
        {
            // JWT format: header.payload.signature
            var parts = idToken.Split('.');
            if (parts.Length != 3)
                return [];

            // Decode payload (base64url)
            var payload = parts[1];
            // Add padding if needed
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            // Replace URL-safe characters
            payload = payload.Replace('-', '+').Replace('_', '/');

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);

            // SECURITY: Validate token expiration before extracting roles
            // The 'exp' claim is a Unix timestamp (seconds since epoch)
            if (document.RootElement.TryGetProperty("exp", out var expElement))
            {
                if (expElement.TryGetInt64(out var expUnixSeconds))
                {
                    var currentUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (expUnixSeconds < currentUnixSeconds)
                    {
                        _logger.LogWarning(
                            "Firebase ID token has expired. Token exp: {TokenExpiration}, Current time: {CurrentTime}. " +
                            "Treating user as unauthenticated for role-based authorization.",
                            DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds),
                            DateTimeOffset.UtcNow);
                        return [];
                    }
                }
            }
            else
            {
                // Tokens without an 'exp' claim should be treated as suspicious
                _logger.LogWarning(
                    "Firebase ID token is missing the 'exp' claim. " +
                    "Treating user as unauthenticated for role-based authorization.");
                return [];
            }

            if (document.RootElement.TryGetProperty(_options.RolesClaimName, out var rolesElement))
            {
                if (rolesElement.ValueKind == JsonValueKind.Array)
                {
                    return rolesElement.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .ToList();
                }
                else if (rolesElement.ValueKind == JsonValueKind.String)
                {
                    return [rolesElement.GetString()!];
                }
            }
        }
        catch (FormatException)
        {
            // Invalid base64 encoding - return no roles
        }
        catch (JsonException)
        {
            // Invalid JSON in token - return no roles
        }

        return [];
    }

    public void Dispose()
    {
        _auth.OnAuthStateChanged -= HandleAuthStateChanged;
    }
}
