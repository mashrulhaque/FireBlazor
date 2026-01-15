using FireBlazor.Components;
using Microsoft.AspNetCore.Components;

namespace FireBlazor.Tests.Components;

public class FirebaseComponentTests
{
    [Fact]
    public void Subscribe_AddsToSubscriptions()
    {
        var component = new TestFirebaseComponent();
        var unsubscribeCalled = false;

        component.TestSubscribe(() => unsubscribeCalled = true);

        // Should have one subscription
        Assert.Equal(1, component.SubscriptionCount);
        Assert.False(unsubscribeCalled);
    }

    [Fact]
    public void Dispose_UnsubscribesAll()
    {
        var component = new TestFirebaseComponent();
        var unsubscribe1Called = false;
        var unsubscribe2Called = false;

        component.TestSubscribe(() => unsubscribe1Called = true);
        component.TestSubscribe(() => unsubscribe2Called = true);

        component.DisposeTest();

        Assert.True(unsubscribe1Called);
        Assert.True(unsubscribe2Called);
    }

    [Fact]
    public void Subscribe_CannotAddAfterDispose()
    {
        var component = new TestFirebaseComponent();

        component.DisposeTest();

        // After dispose, subscriptions should not be added
        component.TestSubscribe(() => { });

        // Subscription count should remain 0 after dispose
        Assert.Equal(0, component.SubscriptionCount);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var component = new TestFirebaseComponent();
        var unsubscribeCount = 0;

        component.TestSubscribe(() => unsubscribeCount++);

        component.DisposeTest();
        component.DisposeTest();
        component.DisposeTest();

        // Should only unsubscribe once
        Assert.Equal(1, unsubscribeCount);
    }

    [Fact]
    public void ClearSubscriptions_UnsubscribesAllWithoutDisposing()
    {
        var component = new TestFirebaseComponent();
        var unsubscribeCalled = false;

        component.TestSubscribe(() => unsubscribeCalled = true);
        component.TestClearSubscriptions();

        Assert.True(unsubscribeCalled);
        Assert.Equal(0, component.SubscriptionCount);

        // Should be able to add new subscriptions after clear
        component.TestSubscribe(() => { });
        Assert.Equal(1, component.SubscriptionCount);
    }
}

/// <summary>
/// Test wrapper to access protected members
/// </summary>
public class TestFirebaseComponent : FirebaseComponentBase
{
    // Use the base class's SubscriptionCount property
    public new int SubscriptionCount => base.SubscriptionCount;

    public void TestSubscribe(Action unsubscribe)
    {
        Subscribe(unsubscribe);
    }

    public void TestClearSubscriptions()
    {
        ClearSubscriptions();
    }

    public void DisposeTest()
    {
        Dispose(true);
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        // Empty for testing
    }
}
