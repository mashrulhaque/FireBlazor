namespace FireBlazor;

/// <summary>
/// Provides special values for Firebase Realtime Database operations.
/// </summary>
public static class ServerValue
{
    /// <summary>
    /// A placeholder value that gets replaced with the server's timestamp when written.
    /// </summary>
    public static readonly object Timestamp = new RtdbServerTimestampValue();

    /// <summary>
    /// Creates an increment value that atomically increments the current value.
    /// </summary>
    /// <param name="delta">The amount to increment by (can be negative for decrement).</param>
    public static object Increment(double delta) => new RtdbIncrementValue(delta);
}

/// <summary>Sentinel for server timestamp in Realtime Database.</summary>
internal sealed class RtdbServerTimestampValue
{
    internal RtdbServerTimestampValue() { }
}

/// <summary>Sentinel for atomic increment in Realtime Database.</summary>
internal sealed class RtdbIncrementValue
{
    public double Delta { get; }
    internal RtdbIncrementValue(double delta) => Delta = delta;
}
