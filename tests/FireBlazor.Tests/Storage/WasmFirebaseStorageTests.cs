using Microsoft.JSInterop;
using NSubstitute;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.Storage;

public class WasmFirebaseStorageTests
{
    [Fact]
    public void Ref_ReturnsStorageReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var storage = new WasmFirebaseStorage(jsInterop);

        var reference = storage.Ref("images/test.jpg");

        Assert.NotNull(reference);
        Assert.Equal("images/test.jpg", reference.FullPath);
        Assert.Equal("test.jpg", reference.Name);
    }

    [Fact]
    public void Ref_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var storage = new WasmFirebaseStorage(jsInterop);

        Assert.ThrowsAny<ArgumentException>(() => storage.Ref(null!));
    }

    [Fact]
    public void Ref_ThrowsOnEmptyPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var storage = new WasmFirebaseStorage(jsInterop);

        Assert.Throws<ArgumentException>(() => storage.Ref(""));
    }

    [Fact]
    public void Ref_ThrowsOnWhitespacePath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var storage = new WasmFirebaseStorage(jsInterop);

        Assert.Throws<ArgumentException>(() => storage.Ref("   "));
    }

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmFirebaseStorage(null!));
    }
}
