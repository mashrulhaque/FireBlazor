namespace FireBlazor;

/// <summary>
/// Configuration options for Firebase authorization integration with Blazor.
/// </summary>
public sealed class FirebaseAuthorizationOptions
{
    /// <summary>
    /// The path to redirect unauthenticated users to. Default: "/login"
    /// </summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>
    /// The path to redirect authenticated but unauthorized users to. Default: "/access-denied"
    /// </summary>
    public string AccessDeniedPath { get; set; } = "/access-denied";

    /// <summary>
    /// The claim name in the Firebase ID token that contains user roles. Default: "roles"
    /// </summary>
    public string RolesClaimName { get; set; } = "roles";
}
