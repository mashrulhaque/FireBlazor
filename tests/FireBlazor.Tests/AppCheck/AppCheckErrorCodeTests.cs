namespace FireBlazor.Tests.AppCheck;

public class AppCheckErrorCodeTests
{
    [Theory]
    [InlineData("appCheck/not-initialized", AppCheckErrorCode.NotInitialized)]
    [InlineData("appCheck/fetch-network-error", AppCheckErrorCode.FetchNetworkError)]
    [InlineData("appCheck/fetch-parse-error", AppCheckErrorCode.FetchParseError)]
    [InlineData("appCheck/fetch-status-error", AppCheckErrorCode.FetchStatusError)]
    [InlineData("appCheck/invalid-configuration", AppCheckErrorCode.InvalidConfiguration)]
    [InlineData("appCheck/storage-open", AppCheckErrorCode.StorageOpen)]
    [InlineData("appCheck/storage-get", AppCheckErrorCode.StorageGet)]
    [InlineData("appCheck/storage-write", AppCheckErrorCode.StorageWrite)]
    [InlineData("appCheck/recaptcha-error", AppCheckErrorCode.RecaptchaError)]
    [InlineData("appCheck/throttled", AppCheckErrorCode.Throttled)]
    [InlineData("appCheck/token-expired", AppCheckErrorCode.TokenExpired)]
    [InlineData("appCheck/attestation-failed", AppCheckErrorCode.AttestationFailed)]
    [InlineData("appCheck/too-many-requests", AppCheckErrorCode.TooManyRequests)]
    [InlineData("appCheck/unknown", AppCheckErrorCode.Unknown)]
    public void FromFirebaseCode_MapsCorrectly(string firebaseCode, AppCheckErrorCode expected)
    {
        var result = AppCheckErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("appCheck/some-new-error")]
    [InlineData("invalid-code")]
    [InlineData("")]
    [InlineData("appCheck/")]
    public void FromFirebaseCode_ReturnsUnknown_ForUnrecognizedCodes(string firebaseCode)
    {
        var result = AppCheckErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(AppCheckErrorCode.Unknown, result);
    }

    [Theory]
    [InlineData(AppCheckErrorCode.Unknown, "appCheck/unknown")]
    [InlineData(AppCheckErrorCode.NotInitialized, "appCheck/not-initialized")]
    [InlineData(AppCheckErrorCode.FetchNetworkError, "appCheck/fetch-network-error")]
    [InlineData(AppCheckErrorCode.FetchParseError, "appCheck/fetch-parse-error")]
    [InlineData(AppCheckErrorCode.FetchStatusError, "appCheck/fetch-status-error")]
    [InlineData(AppCheckErrorCode.InvalidConfiguration, "appCheck/invalid-configuration")]
    [InlineData(AppCheckErrorCode.StorageOpen, "appCheck/storage-open")]
    [InlineData(AppCheckErrorCode.StorageGet, "appCheck/storage-get")]
    [InlineData(AppCheckErrorCode.StorageWrite, "appCheck/storage-write")]
    [InlineData(AppCheckErrorCode.RecaptchaError, "appCheck/recaptcha-error")]
    [InlineData(AppCheckErrorCode.Throttled, "appCheck/throttled")]
    [InlineData(AppCheckErrorCode.TokenExpired, "appCheck/token-expired")]
    [InlineData(AppCheckErrorCode.AttestationFailed, "appCheck/attestation-failed")]
    [InlineData(AppCheckErrorCode.TooManyRequests, "appCheck/too-many-requests")]
    public void ToFirebaseCode_MapsCorrectly(AppCheckErrorCode code, string expected)
    {
        var result = code.ToFirebaseCode();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AppCheckException_ContainsCode()
    {
        var ex = new AppCheckException(AppCheckErrorCode.Throttled, "Too many requests");

        Assert.Equal(AppCheckErrorCode.Throttled, ex.AppCheckCode);
        Assert.Equal("appCheck/throttled", ex.Code);
        Assert.Equal("Too many requests", ex.Message);
    }

    [Fact]
    public void AppCheckException_WithInnerException()
    {
        var innerEx = new InvalidOperationException("Inner error");
        var ex = new AppCheckException(AppCheckErrorCode.RecaptchaError, "ReCaptcha failed", innerEx);

        Assert.Equal(AppCheckErrorCode.RecaptchaError, ex.AppCheckCode);
        Assert.Equal("appCheck/recaptcha-error", ex.Code);
        Assert.Equal("ReCaptcha failed", ex.Message);
        Assert.Same(innerEx, ex.InnerException);
    }

    [Theory]
    [InlineData(AppCheckErrorCode.Unknown)]
    [InlineData(AppCheckErrorCode.NotInitialized)]
    [InlineData(AppCheckErrorCode.FetchNetworkError)]
    [InlineData(AppCheckErrorCode.InvalidConfiguration)]
    [InlineData(AppCheckErrorCode.Throttled)]
    [InlineData(AppCheckErrorCode.TooManyRequests)]
    public void RoundTrip_FromFirebaseCodeToFirebaseCode(AppCheckErrorCode code)
    {
        var firebaseCode = code.ToFirebaseCode();
        var result = AppCheckErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(code, result);
    }
}
