using Microsoft.AspNetCore.Components.Forms;

namespace FireBlazor;

/// <summary>Cloud Storage service interface.</summary>
public interface IFirebaseStorage
{
    /// <summary>
    /// Gets a reference to a storage location.
    /// </summary>
    /// <param name="path">Path to the storage location (e.g., "images/photo.jpg").</param>
    /// <returns>A reference to the storage location.</returns>
    IStorageReference Ref(string path);
}

/// <summary>
/// Represents a reference to a location in Firebase Cloud Storage.
/// </summary>
public interface IStorageReference
{
    /// <summary>
    /// Gets the name of the file or directory (the last segment of the path).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the full path of this reference, not including the bucket.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets a reference to a child location.
    /// </summary>
    /// <param name="path">Path relative to this reference.</param>
    /// <returns>A reference to the child location.</returns>
    IStorageReference Child(string path);

    /// <summary>
    /// Gets a reference to the parent location, or null if this is the root.
    /// </summary>
    IStorageReference? Parent { get; }

    /// <summary>
    /// Uploads data to this reference.
    /// </summary>
    /// <param name="data">The data to upload.</param>
    /// <param name="metadata">Optional metadata for the uploaded file.</param>
    /// <param name="onProgress">Optional callback for upload progress updates.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the upload.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<Result<UploadResult>> PutAsync(Stream data, StorageMetadata? metadata = null, Action<UploadProgress>? onProgress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a browser file to this reference.
    /// </summary>
    /// <param name="file">The browser file to upload.</param>
    /// <param name="metadata">Optional metadata for the uploaded file.</param>
    /// <param name="onProgress">Optional callback for upload progress updates.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the upload.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<Result<UploadResult>> PutAsync(IBrowserFile file, StorageMetadata? metadata = null, Action<UploadProgress>? onProgress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a string to this reference.
    /// </summary>
    /// <param name="data">The string data to upload.</param>
    /// <param name="format">The format of the string (Raw, Base64, Base64Url, DataUrl).</param>
    /// <param name="metadata">Optional metadata for the uploaded file.</param>
    /// <param name="onProgress">Optional callback for upload progress updates.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the upload.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<Result<UploadResult>> PutStringAsync(
        string data,
        StringFormat format = StringFormat.Raw,
        StorageMetadata? metadata = null,
        Action<UploadProgress>? onProgress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the file as a byte array.
    /// </summary>
    /// <param name="maxSize">Maximum size in bytes to download. Defaults to 10 MB.</param>
    /// <returns>The file contents as a byte array.</returns>
    Task<Result<byte[]>> GetBytesAsync(long maxSize = 10 * 1024 * 1024);

    /// <summary>
    /// Downloads the file as a stream.
    /// </summary>
    /// <param name="maxSize">Maximum size in bytes to download. Defaults to 10 MB.</param>
    /// <returns>A stream containing the file data.</returns>
    Task<Result<Stream>> GetStreamAsync(long maxSize = 10 * 1024 * 1024);

    /// <summary>
    /// Gets a public download URL for this file.
    /// </summary>
    /// <returns>The download URL.</returns>
    Task<Result<string>> GetDownloadUrlAsync();

    /// <summary>
    /// Gets the metadata for this file.
    /// </summary>
    /// <returns>The file metadata.</returns>
    Task<Result<StorageMetadata>> GetMetadataAsync();

    /// <summary>
    /// Updates the metadata for this file.
    /// </summary>
    /// <param name="metadata">The new metadata to set.</param>
    /// <returns>The updated metadata.</returns>
    Task<Result<StorageMetadata>> UpdateMetadataAsync(StorageMetadata metadata);

    /// <summary>
    /// Deletes the file at this reference.
    /// </summary>
    /// <returns>The result of the delete operation.</returns>
    Task<Result<Unit>> DeleteAsync();

    /// <summary>
    /// Lists all files and subdirectories under this reference.
    /// </summary>
    /// <returns>The list of files and subdirectories.</returns>
    Task<Result<ListResult>> ListAllAsync();

    /// <summary>
    /// Lists files and subdirectories under this reference with pagination.
    /// </summary>
    /// <param name="options">Options for pagination.</param>
    /// <returns>A page of files and subdirectories.</returns>
    Task<Result<PagedListResult>> ListAsync(ListOptions? options = null);
}

public sealed class StorageMetadata
{
    public string? ContentType { get; set; }
    public string? CacheControl { get; set; }
    public string? ContentDisposition { get; set; }
    public string? ContentEncoding { get; set; }
    public string? ContentLanguage { get; set; }
    public Dictionary<string, string>? CustomMetadata { get; set; }
}

public sealed class UploadResult
{
    public required string DownloadUrl { get; init; }
    public required string FullPath { get; init; }
    public required long BytesTransferred { get; init; }
}

public sealed class UploadProgress
{
    public required long BytesTransferred { get; init; }
    public required long TotalBytes { get; init; }
    public double Percentage => TotalBytes > 0 ? (double)BytesTransferred / TotalBytes * 100 : 0;
}

public sealed class ListResult
{
    public IReadOnlyList<IStorageReference> Items { get; init; } = [];
    public IReadOnlyList<IStorageReference> Prefixes { get; init; } = [];
}

/// <summary>
/// Specifies the format of a string being uploaded to Firebase Storage.
/// </summary>
public enum StringFormat
{
    /// <summary>Raw string (UTF-8 encoded).</summary>
    Raw,
    /// <summary>Base64 encoded string.</summary>
    Base64,
    /// <summary>Base64url encoded string.</summary>
    Base64Url,
    /// <summary>Data URL (e.g., "data:image/png;base64,...").</summary>
    DataUrl
}

/// <summary>
/// Options for paginated listing of storage files.
/// </summary>
public sealed class ListOptions
{
    /// <summary>
    /// Maximum number of results to return. Default is 1000.
    /// </summary>
    public int MaxResults { get; set; } = 1000;

    /// <summary>
    /// Page token from a previous list operation to get next page.
    /// </summary>
    public string? PageToken { get; set; }
}

/// <summary>
/// Result of a paginated list operation.
/// </summary>
public sealed class PagedListResult
{
    /// <summary>Files in this page.</summary>
    public IReadOnlyList<IStorageReference> Items { get; init; } = [];

    /// <summary>Subdirectories (prefixes) in this page.</summary>
    public IReadOnlyList<IStorageReference> Prefixes { get; init; } = [];

    /// <summary>Token to get next page, or null if no more pages.</summary>
    public string? NextPageToken { get; init; }
}
