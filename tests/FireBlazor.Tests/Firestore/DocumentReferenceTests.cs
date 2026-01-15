using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class DocumentReferenceTests
{
    [Fact]
    public void Id_ReturnsCorrectId()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var doc = new WasmDocumentReference<TestDocument>(jsInterop, "users/user123");

        Assert.Equal("user123", doc.Id);
    }

    [Fact]
    public void Path_ReturnsFullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var doc = new WasmDocumentReference<TestDocument>(jsInterop, "users/user123");

        Assert.Equal("users/user123", doc.Path);
    }

    [Fact]
    public void Collection_ReturnsSubcollection()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var doc = new WasmDocumentReference<TestDocument>(jsInterop, "users/user123");

        var subcollection = doc.Collection<PostDocument>("posts");

        Assert.NotNull(subcollection);
        Assert.IsAssignableFrom<ICollectionReference<PostDocument>>(subcollection);
    }

    [Fact]
    public void NestedPath_ExtractsCorrectId()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var doc = new WasmDocumentReference<TestDocument>(jsInterop, "users/user123/posts/post456");

        Assert.Equal("post456", doc.Id);
        Assert.Equal("users/user123/posts/post456", doc.Path);
    }
}

public class PostDocument
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}
