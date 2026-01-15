using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.AppCheck;

public class WasmAppCheckTests
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly IJSRuntime _jsRuntime;

    public WasmAppCheckTests()
    {
        _jsRuntime = Substitute.For<IJSRuntime>();
        _jsInterop = new FirebaseJsInterop(_jsRuntime);
    }

    [Fact]
    public void WasmAppCheck_CanBeConstructed()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        Assert.NotNull(appCheck);
    }

    [Fact]
    public void WasmAppCheck_CanBeConstructedWithOptions()
    {
        var options = new AppCheckOptions().ReCaptchaV3("test-site-key");
        var appCheck = new WasmAppCheck(_jsInterop, options);
        Assert.NotNull(appCheck);
    }

    [Fact]
    public void WasmAppCheck_CurrentToken_InitiallyNull()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        Assert.Null(appCheck.CurrentToken);
    }

    [Fact]
    public void WasmAppCheck_IsActivated_InitiallyFalse()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        Assert.False(appCheck.IsActivated);
    }

    [Fact]
    public void WasmAppCheck_ImplementsIAppCheck()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        Assert.IsAssignableFrom<IAppCheck>(appCheck);
    }

    [Fact]
    public void WasmAppCheck_ImplementsIAsyncDisposable()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        Assert.IsAssignableFrom<IAsyncDisposable>(appCheck);
    }

    [Fact]
    public void WasmAppCheck_TokenAutoRefresh_DoesNotThrow()
    {
        var appCheck = new WasmAppCheck(_jsInterop);

        // Should not throw - this is a no-op in Firebase v9+
#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Record.Exception(() =>
        {
            appCheck.SetTokenAutoRefreshEnabled(true);
            appCheck.SetTokenAutoRefreshEnabled(false);
        });
#pragma warning restore CS0618

        Assert.Null(exception);
    }

    [Fact]
    public void WasmAppCheck_OnTokenChanged_EventCanBeSubscribed()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        AppCheckToken? receivedToken = null;

        appCheck.OnTokenChanged += token => receivedToken = token;

        // Event subscription should not throw
        Assert.Null(receivedToken);
    }

    [Fact]
    public void WasmAppCheck_SubscribeToTokenChanges_ReturnsDisposable()
    {
        var appCheck = new WasmAppCheck(_jsInterop);
        AppCheckToken? receivedToken = null;

        using var subscription = appCheck.SubscribeToTokenChanges(token => receivedToken = token);

        Assert.NotNull(subscription);
        Assert.Null(receivedToken);
    }

    [Fact]
    public void AppCheckOptions_ReCaptchaV3_SetsSiteKey()
    {
        var options = new AppCheckOptions().ReCaptchaV3("my-site-key");

        Assert.Equal("my-site-key", options.ReCaptchaSiteKey);
        Assert.False(options.DebugMode);
    }

    [Fact]
    public void AppCheckOptions_ReCaptchaEnterprise_SetsSiteKey()
    {
        var options = new AppCheckOptions().ReCaptchaEnterprise("enterprise-site-key");

        Assert.Equal("enterprise-site-key", options.ReCaptchaEnterpriseSiteKey);
        Assert.Null(options.ReCaptchaSiteKey);
    }

    [Fact]
    public void AppCheckOptions_Debug_EnablesDebugMode()
    {
        var options = new AppCheckOptions().Debug();

        Assert.True(options.DebugMode);
        Assert.Null(options.DebugToken);
    }

    [Fact]
    public void AppCheckOptions_WithDebugToken_SetsTokenAndEnablesDebugMode()
    {
        var options = new AppCheckOptions().WithDebugToken("12345678-1234-1234-1234-123456789abc");

        Assert.True(options.DebugMode);
        Assert.Equal("12345678-1234-1234-1234-123456789abc", options.DebugToken);
    }

    [Fact]
    public void AppCheckOptions_TokenAutoRefresh_SetsValue()
    {
        var optionsEnabled = new AppCheckOptions().TokenAutoRefresh(true);
        var optionsDisabled = new AppCheckOptions().TokenAutoRefresh(false);

        Assert.True(optionsEnabled.IsTokenAutoRefreshEnabled);
        Assert.False(optionsDisabled.IsTokenAutoRefreshEnabled);
    }

    [Fact]
    public void AppCheckOptions_TokenAutoRefresh_DefaultsToTrue()
    {
        var options = new AppCheckOptions();

        Assert.True(options.IsTokenAutoRefreshEnabled);
    }

    [Fact]
    public void AppCheckOptions_ChainedConfiguration()
    {
        var options = new AppCheckOptions()
            .ReCaptchaV3("test-key")
            .Debug()
            .TokenAutoRefresh(false);

        Assert.Equal("test-key", options.ReCaptchaSiteKey);
        Assert.True(options.DebugMode);
        Assert.False(options.IsTokenAutoRefreshEnabled);
    }

    [Fact]
    public void AppCheckOptions_FullConfiguration()
    {
        var options = new AppCheckOptions()
            .ReCaptchaV3("site-key")
            .WithDebugToken("abcdef12-3456-7890-abcd-ef1234567890")
            .TokenAutoRefresh(true);

        Assert.Equal("site-key", options.ReCaptchaSiteKey);
        Assert.True(options.DebugMode);
        Assert.Equal("abcdef12-3456-7890-abcd-ef1234567890", options.DebugToken);
        Assert.True(options.IsTokenAutoRefreshEnabled);
    }
}
