namespace FireBlazor.Tests.Auth;

public class AuthErrorCodeTests
{
    [Theory]
    [InlineData("auth/invalid-email", AuthErrorCode.InvalidEmail)]
    [InlineData("auth/user-disabled", AuthErrorCode.UserDisabled)]
    [InlineData("auth/user-not-found", AuthErrorCode.UserNotFound)]
    [InlineData("auth/wrong-password", AuthErrorCode.WrongPassword)]
    [InlineData("auth/email-already-in-use", AuthErrorCode.EmailAlreadyInUse)]
    [InlineData("auth/weak-password", AuthErrorCode.WeakPassword)]
    [InlineData("auth/too-many-requests", AuthErrorCode.TooManyRequests)]
    [InlineData("auth/unknown-error", AuthErrorCode.Unknown)]
    public void FromFirebaseCode_MapsCorrectly(string firebaseCode, AuthErrorCode expected)
    {
        var result = AuthErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FirebaseAuthException_ContainsCode()
    {
        var ex = new FirebaseAuthException(AuthErrorCode.InvalidEmail, "Invalid email format");

        Assert.Equal(AuthErrorCode.InvalidEmail, ex.AuthCode);
        Assert.Equal("auth/invalid-email", ex.Code);
        Assert.Equal("Invalid email format", ex.Message);
    }
}
