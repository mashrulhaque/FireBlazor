using Microsoft.JSInterop;
using NSubstitute;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.Storage;

public class WasmStorageReferenceTests
{
    private static WasmStorageReference CreateReference(string path)
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        return new WasmStorageReference(jsInterop, path);
    }

    [Fact]
    public void Name_ReturnsFileNameFromPath()
    {
        var reference = CreateReference("images/photos/test.jpg");
        Assert.Equal("test.jpg", reference.Name);
    }

    [Fact]
    public void Name_ReturnsEntirePathWhenNoSlashes()
    {
        var reference = CreateReference("test.jpg");
        Assert.Equal("test.jpg", reference.Name);
    }

    [Fact]
    public void FullPath_ReturnsOriginalPath()
    {
        var reference = CreateReference("images/photos/test.jpg");
        Assert.Equal("images/photos/test.jpg", reference.FullPath);
    }

    [Fact]
    public void Child_CombinesPathsCorrectly()
    {
        var reference = CreateReference("images");
        var child = reference.Child("photos/test.jpg");

        Assert.Equal("images/photos/test.jpg", child.FullPath);
    }

    [Fact]
    public void Child_HandlesLeadingSlashInChildPath()
    {
        var reference = CreateReference("images");
        var child = reference.Child("/photos/test.jpg");

        Assert.Equal("images/photos/test.jpg", child.FullPath);
    }

    [Fact]
    public void Child_HandlesTrailingSlashInParentPath()
    {
        var reference = CreateReference("images/");
        var child = reference.Child("test.jpg");

        Assert.Equal("images/test.jpg", child.FullPath);
    }

    [Fact]
    public void Child_ThrowsOnNullPath()
    {
        var reference = CreateReference("images");
        Assert.ThrowsAny<ArgumentException>(() => reference.Child(null!));
    }

    [Fact]
    public void Child_ThrowsOnEmptyPath()
    {
        var reference = CreateReference("images");
        Assert.Throws<ArgumentException>(() => reference.Child(""));
    }

    [Fact]
    public void Parent_ReturnsParentReference()
    {
        var reference = CreateReference("images/photos/test.jpg");
        var parent = reference.Parent;

        Assert.NotNull(parent);
        Assert.Equal("images/photos", parent.FullPath);
    }

    [Fact]
    public void Parent_ReturnsNullForRootPath()
    {
        var reference = CreateReference("images");
        var parent = reference.Parent;

        Assert.Null(parent);
    }

    [Fact]
    public void Parent_ReturnsNullForPathWithOnlySlashAtStart()
    {
        var reference = CreateReference("/images");
        var parent = reference.Parent;

        // Parent path would be empty which is invalid, so returns null
        Assert.Null(parent);
    }

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmStorageReference(null!, "path"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        Assert.ThrowsAny<ArgumentException>(() => new WasmStorageReference(jsInterop, null!));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        Assert.Throws<ArgumentException>(() => new WasmStorageReference(jsInterop, ""));
    }

    [Fact]
    public async Task PutAsync_Stream_ThrowsOnNullData()
    {
        var reference = CreateReference("test.jpg");
        await Assert.ThrowsAsync<ArgumentNullException>(() => reference.PutAsync((Stream)null!));
    }

    [Fact]
    public async Task PutAsync_BrowserFile_ThrowsOnNullFile()
    {
        var reference = CreateReference("test.jpg");
        await Assert.ThrowsAsync<ArgumentNullException>(() => reference.PutAsync((Microsoft.AspNetCore.Components.Forms.IBrowserFile)null!));
    }
}
