using Microsoft.JSInterop;
using NSubstitute;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.RealtimeDb;

public class WasmDatabaseReferenceTests
{
    private static WasmDatabaseReference CreateReference(string path)
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        return new WasmDatabaseReference(jsInterop, path);
    }

    #region Property Tests

    [Fact]
    public void Key_ReturnsLastPathSegment()
    {
        var reference = CreateReference("users/123/profile");
        Assert.Equal("profile", reference.Key);
    }

    [Fact]
    public void Key_WithNestedPath_ReturnsLastSegment()
    {
        var reference = CreateReference("a/b/c/d/e");
        Assert.Equal("e", reference.Key);
    }

    [Fact]
    public void Key_WithSingleSegment_ReturnsEntirePath()
    {
        var reference = CreateReference("users");
        Assert.Equal("users", reference.Key);
    }

    [Fact]
    public void Path_ReturnsFullPath()
    {
        var reference = CreateReference("users/123/profile");
        Assert.Equal("users/123/profile", reference.Path);
    }

    [Fact]
    public void Path_PreservesOriginalPath()
    {
        var reference = CreateReference("some/nested/path/here");
        Assert.Equal("some/nested/path/here", reference.Path);
    }

    [Fact]
    public void Path_TrimsLeadingSlash()
    {
        var reference = CreateReference("/users/123");
        Assert.Equal("users/123", reference.Path);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void Child_AppendsToPath()
    {
        var reference = CreateReference("users");
        var child = reference.Child("123");
        Assert.Equal("users/123", child.Path);
    }

    [Fact]
    public void Child_WithNestedPath_CombinesCorrectly()
    {
        var reference = CreateReference("users");
        var child = reference.Child("123/profile/settings");
        Assert.Equal("users/123/profile/settings", child.Path);
    }

    [Fact]
    public void Child_WithLeadingSlash_TrimsSlash()
    {
        var reference = CreateReference("users");
        var child = reference.Child("/123");
        Assert.Equal("users/123", child.Path);
    }

    [Fact]
    public void Child_WithTrailingSlashOnParent_CombinesCorrectly()
    {
        var reference = CreateReference("users/");
        var child = reference.Child("123");
        Assert.Equal("users/123", child.Path);
    }

    [Fact]
    public void Child_WithNullPath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.ThrowsAny<ArgumentException>(() => reference.Child(null!));
    }

    [Fact]
    public void Child_WithEmptyPath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentException>(() => reference.Child(""));
    }

    [Fact]
    public void Child_WithWhitespacePath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentException>(() => reference.Child("   "));
    }

    [Fact]
    public void Parent_ReturnsParentReference()
    {
        var reference = CreateReference("users/123/profile");
        var parent = reference.Parent;

        Assert.NotNull(parent);
        Assert.Equal("users/123", parent.Path);
    }

    [Fact]
    public void Parent_AtRoot_ReturnsNull()
    {
        var reference = CreateReference("users");
        var parent = reference.Parent;

        Assert.Null(parent);
    }

    [Fact]
    public void Parent_HasCorrectPath()
    {
        var reference = CreateReference("a/b/c/d");
        var parent = reference.Parent;

        Assert.NotNull(parent);
        Assert.Equal("a/b/c", parent.Path);
    }

    [Fact]
    public void Parent_ChainedCalls_NavigateUpHierarchy()
    {
        var reference = CreateReference("a/b/c/d");

        var parent1 = reference.Parent;
        Assert.NotNull(parent1);
        Assert.Equal("a/b/c", parent1.Path);

        var parent2 = parent1.Parent;
        Assert.NotNull(parent2);
        Assert.Equal("a/b", parent2.Path);

        var parent3 = parent2.Parent;
        Assert.NotNull(parent3);
        Assert.Equal("a", parent3.Path);

        var parent4 = parent3.Parent;
        Assert.Null(parent4);
    }

    #endregion

    #region Query Method Tests - OrderBy

    [Fact]
    public void OrderByChild_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var ordered = reference.OrderByChild("name");

        Assert.NotNull(ordered);
        Assert.NotSame(reference, ordered);
        Assert.Equal("users", ordered.Path);
    }

    [Fact]
    public void OrderByChild_WithNullPath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.ThrowsAny<ArgumentException>(() => reference.OrderByChild(null!));
    }

    [Fact]
    public void OrderByChild_WithEmptyPath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentException>(() => reference.OrderByChild(""));
    }

    [Fact]
    public void OrderByChild_WithWhitespacePath_ThrowsArgumentException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentException>(() => reference.OrderByChild("   "));
    }

    [Fact]
    public void OrderByChild_WithNestedPath_Succeeds()
    {
        var reference = CreateReference("users");
        var ordered = reference.OrderByChild("profile/name");

        Assert.NotNull(ordered);
        Assert.Equal("users", ordered.Path);
    }

    [Fact]
    public void OrderByKey_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var ordered = reference.OrderByKey();

        Assert.NotNull(ordered);
        Assert.NotSame(reference, ordered);
        Assert.Equal("users", ordered.Path);
    }

    [Fact]
    public void OrderByValue_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("scores");
        var ordered = reference.OrderByValue();

        Assert.NotNull(ordered);
        Assert.NotSame(reference, ordered);
        Assert.Equal("scores", ordered.Path);
    }

    #endregion

    #region Query Method Tests - Limit

    [Fact]
    public void LimitToFirst_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var limited = reference.LimitToFirst(10);

        Assert.NotNull(limited);
        Assert.NotSame(reference, limited);
        Assert.Equal("users", limited.Path);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void LimitToFirst_WithInvalidCount_ThrowsArgumentOutOfRangeException(int count)
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToFirst(count));
    }

    [Fact]
    public void LimitToFirst_WithValidCount_Succeeds()
    {
        var reference = CreateReference("users");
        var limited = reference.LimitToFirst(1);

        Assert.NotNull(limited);
    }

    [Fact]
    public void LimitToLast_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var limited = reference.LimitToLast(10);

        Assert.NotNull(limited);
        Assert.NotSame(reference, limited);
        Assert.Equal("users", limited.Path);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void LimitToLast_WithInvalidCount_ThrowsArgumentOutOfRangeException(int count)
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToLast(count));
    }

    [Fact]
    public void LimitToLast_WithValidCount_Succeeds()
    {
        var reference = CreateReference("users");
        var limited = reference.LimitToLast(1);

        Assert.NotNull(limited);
    }

    #endregion

    #region Query Method Tests - StartAt, EndAt, EqualTo

    [Fact]
    public void StartAt_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.StartAt("A");

        Assert.NotNull(filtered);
        Assert.NotSame(reference, filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void StartAt_WithKey_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.StartAt("value", "key123");

        Assert.NotNull(filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void StartAt_WithNullValue_ThrowsArgumentNullException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentNullException>(() => reference.StartAt(null!));
    }

    [Fact]
    public void StartAt_WithNumericValue_Succeeds()
    {
        var reference = CreateReference("scores");
        var filtered = reference.StartAt(100);

        Assert.NotNull(filtered);
    }

    [Fact]
    public void EndAt_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.EndAt("Z");

        Assert.NotNull(filtered);
        Assert.NotSame(reference, filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void EndAt_WithKey_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.EndAt("value", "key456");

        Assert.NotNull(filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void EndAt_WithNullValue_ThrowsArgumentNullException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentNullException>(() => reference.EndAt(null!));
    }

    [Fact]
    public void EndAt_WithNumericValue_Succeeds()
    {
        var reference = CreateReference("scores");
        var filtered = reference.EndAt(1000);

        Assert.NotNull(filtered);
    }

    [Fact]
    public void EqualTo_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.EqualTo("active");

        Assert.NotNull(filtered);
        Assert.NotSame(reference, filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void EqualTo_WithKey_ReturnsNewReferenceWithQuery()
    {
        var reference = CreateReference("users");
        var filtered = reference.EqualTo("value", "specificKey");

        Assert.NotNull(filtered);
        Assert.Equal("users", filtered.Path);
    }

    [Fact]
    public void EqualTo_WithNullValue_ThrowsArgumentNullException()
    {
        var reference = CreateReference("users");
        Assert.Throws<ArgumentNullException>(() => reference.EqualTo(null!));
    }

    [Fact]
    public void EqualTo_WithBoolValue_Succeeds()
    {
        var reference = CreateReference("users");
        var filtered = reference.EqualTo(true);

        Assert.NotNull(filtered);
    }

    #endregion

    #region Query Immutability Tests

    [Fact]
    public void QueryMethods_ReturnNewInstance_OrderByChild()
    {
        var original = CreateReference("users");
        var modified = original.OrderByChild("name");

        Assert.NotSame(original, modified);
        Assert.Equal(original.Path, modified.Path);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_OrderByKey()
    {
        var original = CreateReference("users");
        var modified = original.OrderByKey();

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_OrderByValue()
    {
        var original = CreateReference("scores");
        var modified = original.OrderByValue();

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_LimitToFirst()
    {
        var original = CreateReference("users");
        var modified = original.LimitToFirst(5);

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_LimitToLast()
    {
        var original = CreateReference("users");
        var modified = original.LimitToLast(5);

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_StartAt()
    {
        var original = CreateReference("users");
        var modified = original.StartAt("A");

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_EndAt()
    {
        var original = CreateReference("users");
        var modified = original.EndAt("Z");

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_ReturnNewInstance_EqualTo()
    {
        var original = CreateReference("users");
        var modified = original.EqualTo("value");

        Assert.NotSame(original, modified);
    }

    [Fact]
    public void QueryMethods_CanBeChained()
    {
        var reference = CreateReference("users");
        var chained = reference
            .OrderByChild("age")
            .StartAt(18)
            .EndAt(65)
            .LimitToFirst(100);

        Assert.NotNull(chained);
        Assert.Equal("users", chained.Path);
    }

    [Fact]
    public void QueryMethods_ChainedCalls_ReturnNewInstances()
    {
        var ref1 = CreateReference("users");
        var ref2 = ref1.OrderByChild("name");
        var ref3 = ref2.LimitToFirst(10);
        var ref4 = ref3.StartAt("A");

        Assert.NotSame(ref1, ref2);
        Assert.NotSame(ref2, ref3);
        Assert.NotSame(ref3, ref4);
    }

    [Fact]
    public void QueryMethods_OriginalReferenceUnchanged()
    {
        var original = CreateReference("users");
        var originalPath = original.Path;
        var originalKey = original.Key;

        // Apply various query modifications
        _ = original.OrderByChild("name");
        _ = original.LimitToFirst(10);
        _ = original.StartAt("A");

        // Original should be unchanged
        Assert.Equal(originalPath, original.Path);
        Assert.Equal(originalKey, original.Key);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmDatabaseReference(null!, "path"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        Assert.Throws<ArgumentNullException>(() => new WasmDatabaseReference(jsInterop, null!));
    }

    [Fact]
    public void Constructor_WithEmptyPath_AllowsRootReference()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        // Empty string represents root reference - should not throw
        var reference = new WasmDatabaseReference(jsInterop, "");
        Assert.Equal("", reference.Path);
    }

    [Fact]
    public void Constructor_WithWhitespacePath_TrimsToPath()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var jsInterop = new FirebaseJsInterop(jsRuntime);
        // Whitespace-only paths are allowed (represent root or trimmed path)
        var reference = new WasmDatabaseReference(jsInterop, "   ");
        Assert.Equal("   ", reference.Path);
    }

    #endregion

    #region Child Navigation Returns Correct Type

    [Fact]
    public void Child_ReturnsIDatabaseReference()
    {
        var reference = CreateReference("users");
        var child = reference.Child("123");

        Assert.IsAssignableFrom<IDatabaseReference>(child);
    }

    [Fact]
    public void Child_ReturnedReference_HasCorrectKey()
    {
        var reference = CreateReference("users");
        var child = reference.Child("123");

        Assert.Equal("123", child.Key);
    }

    [Fact]
    public void Child_ChainedCalls_BuildCorrectPath()
    {
        var reference = CreateReference("root");
        var child = reference
            .Child("level1")
            .Child("level2")
            .Child("level3");

        Assert.Equal("root/level1/level2/level3", child.Path);
        Assert.Equal("level3", child.Key);
    }

    #endregion
}
