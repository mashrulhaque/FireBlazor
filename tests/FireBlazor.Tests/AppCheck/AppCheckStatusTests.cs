using FireBlazor;
using FireBlazor.Testing;

namespace FireBlazor.Tests.AppCheck;

public class AppCheckStatusTests
{
    [Fact]
    public void FakeAppCheck_InitialStatus_IsNotInitialized()
    {
        var appCheck = new FakeAppCheck();
        Assert.Equal(AppCheckStatus.NotInitialized, appCheck.Status);
    }

    [Fact]
    public async Task FakeAppCheck_AfterActivate_StatusIsActive()
    {
        var appCheck = new FakeAppCheck();
        await appCheck.ActivateAsync();
        Assert.Equal(AppCheckStatus.Active, appCheck.Status);
    }

    [Fact]
    public async Task FakeAppCheck_ActivateWithError_StatusIsFailed()
    {
        var appCheck = new FakeAppCheck();
        appCheck.SimulateError(new FirebaseError("appCheck/test-error", "Test error"));

        var result = await appCheck.ActivateAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(AppCheckStatus.Failed, appCheck.Status);
        Assert.NotNull(appCheck.LastError);
        Assert.Equal("appCheck/test-error", appCheck.LastError.Code);
    }

    [Fact]
    public void FakeAppCheck_SimulateStatus_UpdatesStatusAndNotifies()
    {
        var appCheck = new FakeAppCheck();
        var receivedStatuses = new List<AppCheckStatus>();
        appCheck.OnStatusChanged += status => receivedStatuses.Add(status);

        appCheck.SimulateStatus(AppCheckStatus.Initializing);
        appCheck.SimulateStatus(AppCheckStatus.Active);

        Assert.Equal(2, receivedStatuses.Count);
        Assert.Equal(AppCheckStatus.Initializing, receivedStatuses[0]);
        Assert.Equal(AppCheckStatus.Active, receivedStatuses[1]);
    }

    [Fact]
    public void FakeAppCheck_SimulateStatusFailed_SetsLastError()
    {
        var appCheck = new FakeAppCheck();
        var error = new FirebaseError("test/error", "Test message");

        appCheck.SimulateStatus(AppCheckStatus.Failed, error);

        Assert.Equal(AppCheckStatus.Failed, appCheck.Status);
        Assert.Equal(error, appCheck.LastError);
    }

    [Fact]
    public void FakeAppCheck_SimulateStatusActive_ClearsLastError()
    {
        var appCheck = new FakeAppCheck();
        appCheck.SimulateStatus(AppCheckStatus.Failed, new FirebaseError("test", "test"));

        appCheck.SimulateStatus(AppCheckStatus.Active);

        Assert.Null(appCheck.LastError);
    }

    [Fact]
    public void FakeAppCheck_Reset_ClearsStatus()
    {
        var appCheck = new FakeAppCheck();
        appCheck.SimulateStatus(AppCheckStatus.Active);

        appCheck.Reset();

        Assert.Equal(AppCheckStatus.NotInitialized, appCheck.Status);
        Assert.Null(appCheck.LastError);
    }
}
