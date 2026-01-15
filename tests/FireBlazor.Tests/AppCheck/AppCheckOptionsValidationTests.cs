using FireBlazor;

namespace FireBlazor.Tests.AppCheck;

public class AppCheckOptionsValidationTests
{
    [Fact]
    public void ReCaptchaV3_WithEmptySiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentException>(() => options.ReCaptchaV3(""));
    }

    [Fact]
    public void ReCaptchaV3_WithNullSiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentNullException>(() => options.ReCaptchaV3(null!));
    }

    [Fact]
    public void ReCaptchaV3_WithWhitespaceSiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentException>(() => options.ReCaptchaV3("   "));
    }

    [Fact]
    public void ReCaptchaEnterprise_WithEmptySiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentException>(() => options.ReCaptchaEnterprise(""));
    }

    [Fact]
    public void ReCaptchaEnterprise_WithNullSiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentNullException>(() => options.ReCaptchaEnterprise(null!));
    }

    [Fact]
    public void ReCaptchaEnterprise_WithWhitespaceSiteKey_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();
        Assert.Throws<ArgumentException>(() => options.ReCaptchaEnterprise("   "));
    }

    [Fact]
    public void ReCaptchaV3_AfterEnterprise_ThrowsInvalidOperationException()
    {
        var options = new AppCheckOptions();
        options.ReCaptchaEnterprise("enterprise-key");

        var ex = Assert.Throws<InvalidOperationException>(() => options.ReCaptchaV3("v3-key"));
        Assert.Contains("Cannot use both", ex.Message);
    }

    [Fact]
    public void ReCaptchaEnterprise_AfterV3_ThrowsInvalidOperationException()
    {
        var options = new AppCheckOptions();
        options.ReCaptchaV3("v3-key");

        var ex = Assert.Throws<InvalidOperationException>(() => options.ReCaptchaEnterprise("enterprise-key"));
        Assert.Contains("Cannot use both", ex.Message);
    }

    [Fact]
    public void WithDebugToken_InvalidFormat_ThrowsArgumentException()
    {
        var options = new AppCheckOptions();

        var ex = Assert.Throws<ArgumentException>(() => options.WithDebugToken("not-a-uuid"));
        Assert.Contains("UUID format", ex.Message);
    }

    [Fact]
    public void WithDebugToken_ValidUUID_Succeeds()
    {
        var options = new AppCheckOptions();
        var validToken = "12345678-1234-1234-1234-123456789abc";

        options.WithDebugToken(validToken);

        Assert.True(options.DebugMode);
        Assert.Equal(validToken, options.DebugToken);
    }

    [Fact]
    public void AutoDebug_SetsAutoDetectDebugMode()
    {
        var options = new AppCheckOptions();

        options.AutoDebug();

        Assert.True(options.AutoDetectDebugMode);
    }

    [Fact]
    public void Validate_NoProviderConfigured_ThrowsInvalidOperationException()
    {
        var options = new AppCheckOptions();

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("requires a provider", ex.Message);
    }

    [Fact]
    public void Validate_WithReCaptchaEnterprise_Succeeds()
    {
        var options = new AppCheckOptions();
        options.ReCaptchaEnterprise("site-key");

        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithAutoDebug_Succeeds()
    {
        var options = new AppCheckOptions();
        options.AutoDebug();

        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithDebugMode_Succeeds()
    {
        var options = new AppCheckOptions();
        options.Debug();

        options.Validate(); // Should not throw
    }
}
