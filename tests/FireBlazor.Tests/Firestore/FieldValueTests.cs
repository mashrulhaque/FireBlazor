namespace FireBlazor.Tests.Firestore;

public class FieldValueTests
{
    [Fact]
    public void ServerTimestamp_ReturnsServerTimestampSentinel()
    {
        var value = FieldValue.ServerTimestamp();
        Assert.IsType<ServerTimestampValue>(value);
    }

    [Fact]
    public void Increment_WithPositiveValue_ReturnsIncrementSentinel()
    {
        var value = FieldValue.Increment(5);
        var increment = Assert.IsType<IncrementValue>(value);
        Assert.Equal(5, increment.Amount);
    }

    [Fact]
    public void Increment_WithNegativeValue_ReturnsIncrementSentinel()
    {
        var value = FieldValue.Increment(-3);
        var increment = Assert.IsType<IncrementValue>(value);
        Assert.Equal(-3, increment.Amount);
    }

    [Fact]
    public void Increment_WithDouble_ReturnsIncrementSentinel()
    {
        var value = FieldValue.Increment(2.5);
        var increment = Assert.IsType<IncrementDoubleValue>(value);
        Assert.Equal(2.5, increment.Amount);
    }

    [Fact]
    public void ArrayUnion_WithElements_ReturnsArrayUnionSentinel()
    {
        var value = FieldValue.ArrayUnion("a", "b", "c");
        var union = Assert.IsType<ArrayUnionValue>(value);
        Assert.Equal(new[] { "a", "b", "c" }, union.Elements);
    }

    [Fact]
    public void ArrayRemove_WithElements_ReturnsArrayRemoveSentinel()
    {
        var value = FieldValue.ArrayRemove("x", "y");
        var remove = Assert.IsType<ArrayRemoveValue>(value);
        Assert.Equal(new[] { "x", "y" }, remove.Elements);
    }

    [Fact]
    public void Delete_ReturnsDeleteSentinel()
    {
        var value = FieldValue.Delete();
        Assert.IsType<DeleteFieldValue>(value);
    }

    [Fact]
    public void ArrayUnion_WithNullElements_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FieldValue.ArrayUnion(null!));
    }

    [Fact]
    public void ArrayRemove_WithNullElements_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FieldValue.ArrayRemove(null!));
    }
}
