namespace FireBlazor;

/// <summary>
/// Firebase Authentication error codes.
/// </summary>
public enum AuthErrorCode
{
    Unknown,
    InvalidEmail,
    UserDisabled,
    UserNotFound,
    WrongPassword,
    EmailAlreadyInUse,
    WeakPassword,
    TooManyRequests,
    OperationNotAllowed,
    AccountExistsWithDifferentCredential,
    InvalidCredential,
    /// <summary>
    /// Generic invalid credentials error for user-facing scenarios.
    /// Use this instead of UserNotFound/WrongPassword to prevent user enumeration.
    /// </summary>
    InvalidCredentials,
    InvalidVerificationCode,
    InvalidVerificationId,
    RequiresRecentLogin,
    ProviderAlreadyLinked,
    CredentialAlreadyInUse,
    PopupClosedByUser,
    NetworkRequestFailed
}

public static class AuthErrorCodeExtensions
{
    public static AuthErrorCode FromFirebaseCode(string code) => code switch
    {
        "auth/invalid-email" => AuthErrorCode.InvalidEmail,
        "auth/user-disabled" => AuthErrorCode.UserDisabled,
        "auth/user-not-found" => AuthErrorCode.UserNotFound,
        "auth/wrong-password" => AuthErrorCode.WrongPassword,
        "auth/email-already-in-use" => AuthErrorCode.EmailAlreadyInUse,
        "auth/weak-password" => AuthErrorCode.WeakPassword,
        "auth/too-many-requests" => AuthErrorCode.TooManyRequests,
        "auth/operation-not-allowed" => AuthErrorCode.OperationNotAllowed,
        "auth/account-exists-with-different-credential" => AuthErrorCode.AccountExistsWithDifferentCredential,
        "auth/invalid-credential" => AuthErrorCode.InvalidCredential,
        "auth/invalid-verification-code" => AuthErrorCode.InvalidVerificationCode,
        "auth/invalid-verification-id" => AuthErrorCode.InvalidVerificationId,
        "auth/requires-recent-login" => AuthErrorCode.RequiresRecentLogin,
        "auth/provider-already-linked" => AuthErrorCode.ProviderAlreadyLinked,
        "auth/credential-already-in-use" => AuthErrorCode.CredentialAlreadyInUse,
        "auth/popup-closed-by-user" => AuthErrorCode.PopupClosedByUser,
        "auth/network-request-failed" => AuthErrorCode.NetworkRequestFailed,
        _ => AuthErrorCode.Unknown
    };

    public static string ToFirebaseCode(this AuthErrorCode code) => code switch
    {
        AuthErrorCode.InvalidEmail => "auth/invalid-email",
        AuthErrorCode.UserDisabled => "auth/user-disabled",
        AuthErrorCode.UserNotFound => "auth/user-not-found",
        AuthErrorCode.WrongPassword => "auth/wrong-password",
        AuthErrorCode.EmailAlreadyInUse => "auth/email-already-in-use",
        AuthErrorCode.WeakPassword => "auth/weak-password",
        AuthErrorCode.TooManyRequests => "auth/too-many-requests",
        AuthErrorCode.OperationNotAllowed => "auth/operation-not-allowed",
        AuthErrorCode.AccountExistsWithDifferentCredential => "auth/account-exists-with-different-credential",
        AuthErrorCode.InvalidCredential => "auth/invalid-credential",
        AuthErrorCode.InvalidVerificationCode => "auth/invalid-verification-code",
        AuthErrorCode.InvalidVerificationId => "auth/invalid-verification-id",
        AuthErrorCode.RequiresRecentLogin => "auth/requires-recent-login",
        AuthErrorCode.ProviderAlreadyLinked => "auth/provider-already-linked",
        AuthErrorCode.CredentialAlreadyInUse => "auth/credential-already-in-use",
        AuthErrorCode.PopupClosedByUser => "auth/popup-closed-by-user",
        AuthErrorCode.NetworkRequestFailed => "auth/network-request-failed",
        AuthErrorCode.InvalidCredentials => "auth/invalid-credentials",
        _ => "auth/unknown"
    };

    /// <summary>
    /// Gets a user-safe error code that doesn't leak user existence information.
    /// Use this when displaying error messages to end users to prevent user enumeration attacks.
    /// </summary>
    /// <param name="code">The original error code.</param>
    /// <returns>A sanitized error code safe for user-facing scenarios.</returns>
    public static AuthErrorCode ToUserSafeCode(this AuthErrorCode code)
    {
        return code switch
        {
            AuthErrorCode.UserNotFound => AuthErrorCode.InvalidCredentials,
            AuthErrorCode.WrongPassword => AuthErrorCode.InvalidCredentials,
            _ => code
        };
    }
}
