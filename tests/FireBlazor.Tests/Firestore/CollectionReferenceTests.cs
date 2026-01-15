using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class CollectionReferenceTests
{
    [Fact]
    public void Document_ReturnsDocumentReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var doc = collection.Document("user123");

        Assert.NotNull(doc);
        Assert.Equal("user123", doc.Id);
        Assert.Equal("users/user123", doc.Path);
    }

    [Fact]
    public void Where_ReturnsNewCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var filtered = collection.Where(x => x.Age > 18);

        Assert.NotNull(filtered);
        Assert.IsAssignableFrom<ICollectionReference<TestDocument>>(filtered);
    }

    [Fact]
    public void OrderBy_ReturnsNewCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var ordered = collection.OrderBy(x => x.Name);

        Assert.NotNull(ordered);
        Assert.IsAssignableFrom<ICollectionReference<TestDocument>>(ordered);
    }

    [Fact]
    public void Take_ReturnsNewCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var limited = collection.Take(10);

        Assert.NotNull(limited);
    }

    [Fact]
    public void Skip_ThrowsNotSupportedException()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var ex = Assert.Throws<NotSupportedException>(() => collection.Skip(10));
        Assert.Contains("cursor-based pagination", ex.Message);
    }

    [Fact]
    public void ChainedQueries_ReturnCollectionReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        var result = collection
            .Where(x => x.Age >= 21)
            .OrderBy(x => x.Name)
            .Take(5);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<ICollectionReference<TestDocument>>(result);
    }

    [Fact]
    public void OnSnapshot_ReturnsUnsubscribeAction()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        // OnSnapshot now starts an async subscription and returns an unsubscribe action
        var unsubscribe = collection.OnSnapshot(
            _ => { },
            error => { }
        );

        // Unsubscribe action should be returned (real subscription is async)
        Assert.NotNull(unsubscribe);

        // Calling unsubscribe should not throw
        unsubscribe();
    }

    [Fact]
    public void Constructor_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);

        Assert.Throws<ArgumentNullException>(() => new WasmCollectionReference<TestDocument>(jsInterop, null!));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);

        Assert.Throws<ArgumentException>(() => new WasmCollectionReference<TestDocument>(jsInterop, ""));
    }

    [Fact]
    public void Document_ThrowsOnNullId()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var collection = new WasmCollectionReference<TestDocument>(jsInterop, "users");

        Assert.Throws<ArgumentNullException>(() => collection.Document(null!));
    }
}
