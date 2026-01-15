namespace FireBlazor;

/// <summary>
/// Sentinel values for Firestore field operations.
/// These are transformed into server-side operations during writes.
/// </summary>
public abstract class FieldValue
{
    internal FieldValue() { }

    /// <summary>
    /// Returns a sentinel for use with SetAsync() or UpdateAsync() to include
    /// a server-generated timestamp in the written data.
    /// </summary>
    public static FieldValue ServerTimestamp() => new ServerTimestampValue();

    /// <summary>
    /// Returns a sentinel for use with SetAsync() or UpdateAsync() to increment
    /// a numeric field by the given amount.
    /// </summary>
    public static FieldValue Increment(long amount) => new IncrementValue(amount);

    /// <summary>
    /// Returns a sentinel for use with SetAsync() or UpdateAsync() to increment
    /// a numeric field by the given double amount.
    /// </summary>
    public static FieldValue Increment(double amount) => new IncrementDoubleValue(amount);

    /// <summary>
    /// Returns a sentinel for use with SetAsync() or UpdateAsync() to add elements
    /// to an array field. Elements that already exist are not added.
    /// </summary>
    public static FieldValue ArrayUnion(params object[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        return new ArrayUnionValue(elements);
    }

    /// <summary>
    /// Returns a sentinel for use with SetAsync() or UpdateAsync() to remove elements
    /// from an array field.
    /// </summary>
    public static FieldValue ArrayRemove(params object[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        return new ArrayRemoveValue(elements);
    }

    /// <summary>
    /// Returns a sentinel for use with UpdateAsync() to mark a field for deletion.
    /// </summary>
    public static FieldValue Delete() => new DeleteFieldValue();
}

/// <summary>
/// Sentinel value that sets a field to the server timestamp.
/// </summary>
public sealed class ServerTimestampValue : FieldValue
{
    internal ServerTimestampValue() { }
}

/// <summary>
/// Sentinel value that increments a numeric field by a long amount.
/// </summary>
public sealed class IncrementValue : FieldValue
{
    /// <summary>
    /// The amount to increment by.
    /// </summary>
    public long Amount { get; }

    internal IncrementValue(long amount) => Amount = amount;
}

/// <summary>
/// Sentinel value that increments a numeric field by a double amount.
/// </summary>
public sealed class IncrementDoubleValue : FieldValue
{
    /// <summary>
    /// The amount to increment by.
    /// </summary>
    public double Amount { get; }

    internal IncrementDoubleValue(double amount) => Amount = amount;
}

/// <summary>
/// Sentinel value that adds elements to an array field (union operation).
/// </summary>
public sealed class ArrayUnionValue : FieldValue
{
    /// <summary>
    /// The elements to add to the array.
    /// </summary>
    public object[] Elements { get; }

    internal ArrayUnionValue(object[] elements) => Elements = elements;
}

/// <summary>
/// Sentinel value that removes elements from an array field.
/// </summary>
public sealed class ArrayRemoveValue : FieldValue
{
    /// <summary>
    /// The elements to remove from the array.
    /// </summary>
    public object[] Elements { get; }

    internal ArrayRemoveValue(object[] elements) => Elements = elements;
}

/// <summary>
/// Sentinel value that marks a field for deletion.
/// </summary>
public sealed class DeleteFieldValue : FieldValue
{
    internal DeleteFieldValue() { }
}
