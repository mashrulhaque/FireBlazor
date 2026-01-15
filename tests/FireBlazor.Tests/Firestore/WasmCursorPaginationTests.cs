using NSubstitute;
using Microsoft.JSInterop;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.Firestore;

public class WasmCursorPaginationTests
{
    public class Item
    {
        public string? Name { get; set; }
        public int Price { get; set; }
    }

    [Fact]
    public void StartAt_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .OrderBy(x => x.Price)
            .StartAt(100);

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public void StartAfter_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .OrderBy(x => x.Price)
            .StartAfter(100);

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public void EndAt_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .OrderBy(x => x.Price)
            .EndAt(500);

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public void EndBefore_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .OrderBy(x => x.Price)
            .EndBefore(500);

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public void Cursors_CanBeChainedWithOtherMethods()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .Where(x => x.Name != null)
            .OrderBy(x => x.Price)
            .StartAfter(100)
            .EndBefore(500)
            .Take(10);

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public void StartAt_WithNullFieldValues_ThrowsArgumentNullException()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<Item>("items").OrderBy(x => x.Price);

        Assert.Throws<ArgumentNullException>(() => collection.StartAt(null!));
    }

    [Fact]
    public void StartAfter_WithEmptyFieldValues_ThrowsArgumentException()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<Item>("items").OrderBy(x => x.Price);

        Assert.Throws<ArgumentException>(() => collection.StartAfter());
    }

    [Fact]
    public void StartAt_WithMultipleValues_ReturnsCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var result = firestore.Collection<Item>("items")
            .OrderBy(x => x.Price)
            .OrderBy(x => x.Name)
            .StartAt(100, "A");

        Assert.IsAssignableFrom<ICollectionReference<Item>>(result);
    }

    [Fact]
    public async Task GetAsync_WithCursorsButNoOrderBy_ThrowsInvalidOperationException()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        var collection = firestore.Collection<Item>("items").StartAfter(100);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => collection.GetAsync());

        Assert.Contains("OrderBy", exception.Message);
    }
}
