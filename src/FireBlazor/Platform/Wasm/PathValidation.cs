namespace FireBlazor.Platform.Wasm;

/// <summary>
/// Provides path validation utilities to prevent path traversal attacks.
/// </summary>
internal static class PathValidation
{
    /// <summary>
    /// Validates that a path segment or identifier does not contain path traversal sequences.
    /// </summary>
    /// <param name="path">The path or identifier to validate.</param>
    /// <param name="paramName">The parameter name for exception messages.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the path is null, empty, whitespace, or contains path traversal sequences.
    /// </exception>
    public static void ValidatePath(string path, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);

        // Reject null bytes which can truncate paths in some systems
        if (path.Contains('\0'))
            throw new ArgumentException("Invalid path: contains null characters", paramName);

        // Reject path traversal sequences
        if (path.Contains(".."))
            throw new ArgumentException("Invalid path: contains path traversal sequence", paramName);

        // Reject double slashes which could indicate path manipulation
        if (path.Contains("//"))
            throw new ArgumentException("Invalid path: contains invalid sequence", paramName);

        // Note: Leading/trailing slashes are allowed and trimmed by the calling code.
        // This matches Firebase SDK behavior which normalizes paths gracefully.
    }
}
