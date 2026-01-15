using Microsoft.JSInterop;
using NSubstitute;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.RealtimeDb;

public class WasmRealtimeDatabaseTests
{
    private readonly IJSRuntime _jsRuntime;
    private readonly FirebaseJsInterop _jsInterop;

    public WasmRealtimeDatabaseTests()
    {
        _jsRuntime = Substitute.For<IJSRuntime>();
        _jsInterop = new FirebaseJsInterop(_jsRuntime);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullJsInterop()
    {
        Assert.Throws<ArgumentNullException>(() => new WasmRealtimeDatabase(null!));
    }

    [Fact]
    public void Constructor_WithValidJsInterop_Succeeds()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        Assert.NotNull(db);
    }

    #endregion

    #region Ref() Method Tests

    [Fact]
    public void Ref_WithValidPath_ReturnsReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users/123");

        Assert.NotNull(reference);
    }

    [Fact]
    public void Ref_WithNullPath_ThrowsArgumentException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        Assert.ThrowsAny<ArgumentException>(() => db.Ref(null!));
    }

    [Fact]
    public void Ref_WithEmptyPath_ReturnsRootReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("");

        Assert.NotNull(reference);
        Assert.Equal("", reference.Path);
        Assert.Equal("", reference.Key);
    }

    [Fact]
    public void Ref_WithWhitespacePath_PreservesWhitespace()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        // Whitespace is preserved (only leading slashes are trimmed)
        var reference = db.Ref("   ");

        Assert.NotNull(reference);
        Assert.Equal("   ", reference.Path);
    }

    [Fact]
    public void Ref_ReturnsReferenceWithCorrectPath()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users/123/profile");

        Assert.Equal("users/123/profile", reference.Path);
    }

    [Fact]
    public void Ref_WithLeadingSlash_TrimsSlash()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("/users/123");

        Assert.Equal("users/123", reference.Path);
    }

    #endregion

    #region Reference Property Tests

    [Fact]
    public void Ref_Reference_HasCorrectKey_SimpleSegment()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users");

        Assert.Equal("users", reference.Key);
    }

    [Fact]
    public void Ref_Reference_HasCorrectKey_MultipleSegments()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users/123/profile");

        Assert.Equal("profile", reference.Key);
    }

    [Fact]
    public void Ref_Reference_HasCorrectPath()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users/123");

        Assert.Equal("users/123", reference.Path);
    }

    [Fact]
    public void Ref_Reference_Parent_ReturnsParentReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users/123/profile");
        var parent = reference.Parent;

        Assert.NotNull(parent);
        Assert.Equal("users/123", parent!.Path);
    }

    [Fact]
    public void Ref_Reference_Parent_ReturnsNull_ForRootLevelPath()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users");
        var parent = reference.Parent;

        Assert.Null(parent);
    }

    [Fact]
    public void Ref_Reference_Child_AppendsPath()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users");
        var child = reference.Child("123");

        Assert.Equal("users/123", child.Path);
        Assert.Equal("123", child.Key);
    }

    [Fact]
    public void Ref_Reference_Child_WithNullPath_ThrowsArgumentException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users");

        Assert.ThrowsAny<ArgumentException>(() => reference.Child(null!));
    }

    [Fact]
    public void Ref_Reference_Child_WithEmptyPath_ThrowsArgumentException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);

        var reference = db.Ref("users");

        Assert.Throws<ArgumentException>(() => reference.Child(""));
    }

    #endregion

    #region Query Method Tests

    [Fact]
    public void OrderByChild_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var orderedRef = reference.OrderByChild("name");

        Assert.NotNull(orderedRef);
        Assert.NotSame(reference, orderedRef);
        Assert.Equal("users", orderedRef.Path);
    }

    [Fact]
    public void OrderByChild_WithNullPath_ThrowsArgumentException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.ThrowsAny<ArgumentException>(() => reference.OrderByChild(null!));
    }

    [Fact]
    public void OrderByKey_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var orderedRef = reference.OrderByKey();

        Assert.NotNull(orderedRef);
        Assert.NotSame(reference, orderedRef);
        Assert.Equal("users", orderedRef.Path);
    }

    [Fact]
    public void OrderByValue_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var orderedRef = reference.OrderByValue();

        Assert.NotNull(orderedRef);
        Assert.NotSame(reference, orderedRef);
        Assert.Equal("users", orderedRef.Path);
    }

    [Fact]
    public void LimitToFirst_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var limitedRef = reference.LimitToFirst(10);

        Assert.NotNull(limitedRef);
        Assert.NotSame(reference, limitedRef);
        Assert.Equal("users", limitedRef.Path);
    }

    [Fact]
    public void LimitToFirst_WithZeroCount_ThrowsArgumentOutOfRangeException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToFirst(0));
    }

    [Fact]
    public void LimitToFirst_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToFirst(-1));
    }

    [Fact]
    public void LimitToLast_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var limitedRef = reference.LimitToLast(10);

        Assert.NotNull(limitedRef);
        Assert.NotSame(reference, limitedRef);
        Assert.Equal("users", limitedRef.Path);
    }

    [Fact]
    public void LimitToLast_WithZeroCount_ThrowsArgumentOutOfRangeException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToLast(0));
    }

    [Fact]
    public void LimitToLast_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentOutOfRangeException>(() => reference.LimitToLast(-1));
    }

    [Fact]
    public void StartAt_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var filteredRef = reference.StartAt("abc");

        Assert.NotNull(filteredRef);
        Assert.NotSame(reference, filteredRef);
        Assert.Equal("users", filteredRef.Path);
    }

    [Fact]
    public void StartAt_WithNullValue_ThrowsArgumentNullException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentNullException>(() => reference.StartAt(null!));
    }

    [Fact]
    public void EndAt_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var filteredRef = reference.EndAt("xyz");

        Assert.NotNull(filteredRef);
        Assert.NotSame(reference, filteredRef);
        Assert.Equal("users", filteredRef.Path);
    }

    [Fact]
    public void EndAt_WithNullValue_ThrowsArgumentNullException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentNullException>(() => reference.EndAt(null!));
    }

    [Fact]
    public void EqualTo_ReturnsNewReference()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var filteredRef = reference.EqualTo("test-value");

        Assert.NotNull(filteredRef);
        Assert.NotSame(reference, filteredRef);
        Assert.Equal("users", filteredRef.Path);
    }

    [Fact]
    public void EqualTo_WithNullValue_ThrowsArgumentNullException()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        Assert.Throws<ArgumentNullException>(() => reference.EqualTo(null!));
    }

    #endregion

    #region Query Chaining Tests

    [Fact]
    public void QueryMethods_CanBeChained()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var reference = db.Ref("users");

        var chainedRef = reference
            .OrderByChild("name")
            .StartAt("A")
            .EndAt("Z")
            .LimitToFirst(10);

        Assert.NotNull(chainedRef);
        Assert.Equal("users", chainedRef.Path);
    }

    [Fact]
    public void QueryMethods_AreImmutable()
    {
        var db = new WasmRealtimeDatabase(_jsInterop);
        var originalRef = db.Ref("users");

        var orderedRef = originalRef.OrderByChild("name");
        var limitedRef = orderedRef.LimitToFirst(5);

        // Original references should be unchanged
        Assert.NotSame(originalRef, orderedRef);
        Assert.NotSame(orderedRef, limitedRef);
        Assert.NotSame(originalRef, limitedRef);
    }

    #endregion

    #region DataSnapshot Tests

    [Fact]
    public void DataSnapshot_HasExpectedProperties()
    {
        var snapshot = new DataSnapshot<string>
        {
            Key = "test-key",
            Exists = true,
            Value = "test-value"
        };

        Assert.Equal("test-key", snapshot.Key);
        Assert.True(snapshot.Exists);
        Assert.Equal("test-value", snapshot.Value);
    }

    [Fact]
    public void DataSnapshot_ValueAs_ReturnsCorrectType()
    {
        var snapshot = new DataSnapshot<int>
        {
            Key = "test",
            Exists = true,
            Value = 42
        };

        var value = snapshot.ValueAs<int>();

        Assert.Equal(42, value);
    }

    [Fact]
    public void DataSnapshot_ValueAs_ReturnsDefaultOnMismatch()
    {
        var snapshot = new DataSnapshot<string>
        {
            Key = "test",
            Exists = true,
            Value = "hello"
        };

        var value = snapshot.ValueAs<int>();

        Assert.Equal(default, value);
    }

    [Fact]
    public void DataSnapshot_WithNullValue_ReturnsNull()
    {
        var snapshot = new DataSnapshot<string>
        {
            Key = "test",
            Exists = false,
            Value = null
        };

        Assert.Null(snapshot.Value);
        Assert.False(snapshot.Exists);
    }

    #endregion
}
