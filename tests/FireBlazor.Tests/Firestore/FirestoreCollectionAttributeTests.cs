using FireBlazor.Platform.Wasm;
using Microsoft.JSInterop;
using NSubstitute;

namespace FireBlazor.Tests.Firestore;

public class FirestoreCollectionAttributeTests
{
    [Fact]
    public void Attribute_StoresCollectionPath()
    {
        var attr = new FirestoreCollectionAttribute("users");

        Assert.Equal("users", attr.CollectionPath);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        var attrs = typeof(UserEntity).GetCustomAttributes(typeof(FirestoreCollectionAttribute), false);

        Assert.Single(attrs);
        var attr = (FirestoreCollectionAttribute)attrs[0];
        Assert.Equal("users", attr.CollectionPath);
    }

    [Fact]
    public void GetCollectionPath_ReturnsPath_WhenAttributeExists()
    {
        var path = FirestoreCollectionAttribute.GetCollectionPath<UserEntity>();

        Assert.Equal("users", path);
    }

    [Fact]
    public void GetCollectionPath_ReturnsNull_WhenNoAttribute()
    {
        var path = FirestoreCollectionAttribute.GetCollectionPath<EntityWithoutAttribute>();

        Assert.Null(path);
    }

    [Fact]
    public void GetCollectionPath_ReturnsPath_FromNestedType()
    {
        var path = FirestoreCollectionAttribute.GetCollectionPath<PostEntity>();

        Assert.Equal("posts", path);
    }

    [Fact]
    public void FirestoreExtension_Collection_UsesAttribute()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        // Collection<T>() without path should use the attribute
        var collection = firestore.Collection<UserEntity>();

        Assert.NotNull(collection);
    }

    [Fact]
    public void FirestoreExtension_Collection_ThrowsWithoutAttribute()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        var firestore = new WasmFirestore(jsInterop);

        // Should throw for types without the attribute
        var ex = Assert.Throws<InvalidOperationException>(() => firestore.Collection<EntityWithoutAttribute>());
        Assert.Contains("FirestoreCollectionAttribute", ex.Message);
    }
}

[FirestoreCollection("users")]
public class UserEntity
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

[FirestoreCollection("posts")]
public class PostEntity
{
    public string? Id { get; set; }
    public string? Title { get; set; }
}

public class EntityWithoutAttribute
{
    public string? Id { get; set; }
}
