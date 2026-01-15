// tests/FireBlazor.Tests/Firestore/CursorQueryInterfaceTests.cs
namespace FireBlazor.Tests.Firestore;

public class CursorQueryInterfaceTests
{
    public class Item { public string? Name { get; set; } }

    [Fact]
    public void ICollectionReference_HasStartAtMethod()
    {
        var method = typeof(ICollectionReference<Item>).GetMethod("StartAt");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICollectionReference_HasStartAfterMethod()
    {
        var method = typeof(ICollectionReference<Item>).GetMethod("StartAfter");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICollectionReference_HasEndAtMethod()
    {
        var method = typeof(ICollectionReference<Item>).GetMethod("EndAt");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICollectionReference_HasEndBeforeMethod()
    {
        var method = typeof(ICollectionReference<Item>).GetMethod("EndBefore");
        Assert.NotNull(method);
    }
}
