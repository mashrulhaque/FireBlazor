namespace FireBlazor.Tests.AILogic;

public class AILogicErrorCodeTests
{
    [Theory]
    [InlineData("ai/invalid-api-key", AILogicErrorCode.InvalidApiKey)]
    [InlineData("ai/quota-exceeded", AILogicErrorCode.QuotaExceeded)]
    [InlineData("ai/model-not-found", AILogicErrorCode.ModelNotFound)]
    [InlineData("ai/invalid-request", AILogicErrorCode.InvalidRequest)]
    [InlineData("ai/content-blocked", AILogicErrorCode.ContentBlocked)]
    [InlineData("ai/network-error", AILogicErrorCode.NetworkError)]
    [InlineData("ai/timeout", AILogicErrorCode.Timeout)]
    [InlineData("ai/service-unavailable", AILogicErrorCode.ServiceUnavailable)]
    [InlineData("ai/something-unknown", AILogicErrorCode.Unknown)]
    public void FromFirebaseCode_MapsCorrectly(string firebaseCode, AILogicErrorCode expected)
    {
        var result = AILogicErrorCodeExtensions.FromFirebaseCode(firebaseCode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AILogicErrorCode.InvalidApiKey, "ai/invalid-api-key")]
    [InlineData(AILogicErrorCode.QuotaExceeded, "ai/quota-exceeded")]
    [InlineData(AILogicErrorCode.Unknown, "ai/unknown")]
    public void ToFirebaseCode_MapsCorrectly(AILogicErrorCode code, string expected)
    {
        var result = code.ToFirebaseCode();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AILogicException_ContainsCode()
    {
        var ex = new AILogicException(AILogicErrorCode.QuotaExceeded, "Rate limit exceeded");

        Assert.Equal(AILogicErrorCode.QuotaExceeded, ex.AICode);
        Assert.Equal("ai/quota-exceeded", ex.Code);
        Assert.Equal("Rate limit exceeded", ex.Message);
    }

    [Fact]
    public void AILogicException_WithInnerException_PreservesDetails()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AILogicException(AILogicErrorCode.NetworkError, "Network failed", inner);

        Assert.Equal(AILogicErrorCode.NetworkError, ex.AICode);
        Assert.Same(inner, ex.InnerException);
    }
}
