using Microsoft.AspNetCore.Components.Forms;

namespace FireBlazor.Testing;

/// <summary>
/// In-memory fake implementation of IFirebaseStorage for testing.
/// </summary>
/// <remarks>
/// This fake is designed for single-threaded unit tests.
/// It is NOT thread-safe for concurrent access from multiple threads.
/// </remarks>
public sealed class FakeFirebaseStorage : IFirebaseStorage
{
    private readonly Dictionary<string, (byte[] Data, StorageMetadata? Metadata)> _files = new();
    private FirebaseError? _simulatedError;

    public IStorageReference Ref(string path)
    {
        return new FakeStorageReference(this, path);
    }

    /// <summary>
    /// Simulates an error for the next operation.
    /// </summary>
    public void SimulateError(FirebaseError error)
    {
        _simulatedError = error;
    }

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        _files.Clear();
        _simulatedError = null;
    }

    internal bool TryConsumeSimulatedError(out FirebaseError? error)
    {
        error = _simulatedError;
        _simulatedError = null;
        return error != null;
    }

    internal void StoreFile(string path, byte[] data, StorageMetadata? metadata)
    {
        _files[path] = (data, metadata);
    }

    internal (byte[] Data, StorageMetadata? Metadata)? GetFile(string path)
    {
        return _files.TryGetValue(path, out var file) ? file : null;
    }

    internal bool FileExists(string path) => _files.ContainsKey(path);

    internal void DeleteFile(string path) => _files.Remove(path);

    internal IEnumerable<string> ListFiles(string prefix)
    {
        var normalizedPrefix = prefix.EndsWith("/") ? prefix : prefix + "/";
        return _files.Keys.Where(k => k.StartsWith(normalizedPrefix));
    }
}

internal sealed class FakeStorageReference : IStorageReference
{
    private readonly FakeFirebaseStorage _storage;
    private readonly string _path;

    public FakeStorageReference(FakeFirebaseStorage storage, string path)
    {
        _storage = storage;
        _path = NormalizePath(path);
    }

    public string Name => _path.Contains('/') ? _path.Split('/').Last() : _path;
    public string FullPath => _path;

    public IStorageReference Child(string path)
    {
        var combinedPath = $"{_path.TrimEnd('/')}/{path.TrimStart('/')}";
        return new FakeStorageReference(_storage, combinedPath);
    }

    public IStorageReference? Parent
    {
        get
        {
            if (!_path.Contains('/'))
                return null;
            var parentPath = _path.Substring(0, _path.LastIndexOf('/'));
            return new FakeStorageReference(_storage, parentPath);
        }
    }

    public async Task<Result<UploadResult>> PutAsync(Stream data, StorageMetadata? metadata = null, Action<UploadProgress>? onProgress = null, CancellationToken cancellationToken = default)
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Result<UploadResult>.Failure(error!);

        cancellationToken.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();
        await data.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        // Report progress
        if (onProgress != null)
        {
            onProgress(new UploadProgress { BytesTransferred = bytes.Length / 2, TotalBytes = bytes.Length });
            onProgress(new UploadProgress { BytesTransferred = bytes.Length, TotalBytes = bytes.Length });
        }

        _storage.StoreFile(_path, bytes, metadata);

        return Result<UploadResult>.Success(new UploadResult
        {
            DownloadUrl = $"https://fake-storage.example.com/{_path}",
            FullPath = _path,
            BytesTransferred = bytes.Length
        });
    }

    public Task<Result<UploadResult>> PutAsync(IBrowserFile file, StorageMetadata? metadata = null, Action<UploadProgress>? onProgress = null, CancellationToken cancellationToken = default)
    {
        // For testing, we can't easily use IBrowserFile, so return a mock result
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<UploadResult>.Failure(error!));

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<UploadResult>.Success(new UploadResult
        {
            DownloadUrl = $"https://fake-storage.example.com/{_path}",
            FullPath = _path,
            BytesTransferred = file.Size
        }));
    }

    public Task<Result<byte[]>> GetBytesAsync(long maxSize = 10 * 1024 * 1024)
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<byte[]>.Failure(error!));

        var file = _storage.GetFile(_path);
        if (file == null)
            return Task.FromResult(Result<byte[]>.Failure(new FirebaseError("storage/object-not-found", "Object not found")));

        if (file.Value.Data.Length > maxSize)
            return Task.FromResult(Result<byte[]>.Failure(new FirebaseError("storage/retry-limit-exceeded", "File too large")));

        return Task.FromResult(Result<byte[]>.Success(file.Value.Data));
    }

    public Task<Result<string>> GetDownloadUrlAsync()
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<string>.Failure(error!));

        if (!_storage.FileExists(_path))
            return Task.FromResult(Result<string>.Failure(new FirebaseError("storage/object-not-found", "Object not found")));

        return Task.FromResult(Result<string>.Success($"https://fake-storage.example.com/{_path}"));
    }

    public Task<Result<StorageMetadata>> GetMetadataAsync()
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<StorageMetadata>.Failure(error!));

        var file = _storage.GetFile(_path);
        if (file == null)
            return Task.FromResult(Result<StorageMetadata>.Failure(new FirebaseError("storage/object-not-found", "Object not found")));

        return Task.FromResult(Result<StorageMetadata>.Success(file.Value.Metadata ?? new StorageMetadata()));
    }

    public Task<Result<Unit>> DeleteAsync()
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<Unit>.Failure(error!));

        _storage.DeleteFile(_path);
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }

    public Task<Result<ListResult>> ListAllAsync()
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<ListResult>.Failure(error!));

        var files = _storage.ListFiles(_path);
        var items = files.Select(f => (IStorageReference)new FakeStorageReference(_storage, f)).ToList();

        return Task.FromResult(Result<ListResult>.Success(new ListResult
        {
            Items = items,
            Prefixes = []
        }));
    }

    public async Task<Result<UploadResult>> PutStringAsync(
        string data,
        StringFormat format = StringFormat.Raw,
        StorageMetadata? metadata = null,
        Action<UploadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Result<UploadResult>.Failure(error!);

        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes = format switch
        {
            StringFormat.Base64 => Convert.FromBase64String(data),
            StringFormat.Base64Url => Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/')),
            StringFormat.DataUrl => Convert.FromBase64String(data.Substring(data.IndexOf(',') + 1)),
            _ => System.Text.Encoding.UTF8.GetBytes(data)
        };

        onProgress?.Invoke(new UploadProgress { BytesTransferred = bytes.Length, TotalBytes = bytes.Length });

        _storage.StoreFile(_path, bytes, metadata);

        return await Task.FromResult(Result<UploadResult>.Success(new UploadResult
        {
            DownloadUrl = $"https://fake-storage.example.com/{_path}",
            FullPath = _path,
            BytesTransferred = bytes.Length
        }));
    }

    public Task<Result<StorageMetadata>> UpdateMetadataAsync(StorageMetadata metadata)
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<StorageMetadata>.Failure(error!));

        var file = _storage.GetFile(_path);
        if (file == null)
            return Task.FromResult(Result<StorageMetadata>.Failure(
                new FirebaseError("storage/object-not-found", "Object not found")));

        _storage.StoreFile(_path, file.Value.Data, metadata);

        return Task.FromResult(Result<StorageMetadata>.Success(metadata));
    }

    public Task<Result<PagedListResult>> ListAsync(ListOptions? options = null)
    {
        if (_storage.TryConsumeSimulatedError(out var error))
            return Task.FromResult(Result<PagedListResult>.Failure(error!));

        var maxResults = options?.MaxResults ?? 1000;
        var files = _storage.ListFiles(_path).ToList();

        var startIndex = 0;
        if (!string.IsNullOrEmpty(options?.PageToken) && int.TryParse(options.PageToken, out var idx))
        {
            startIndex = idx;
        }

        var pageFiles = files.Skip(startIndex).Take(maxResults).ToList();
        var hasMore = startIndex + maxResults < files.Count;

        var items = pageFiles.Select(f => (IStorageReference)new FakeStorageReference(_storage, f)).ToList();

        return Task.FromResult(Result<PagedListResult>.Success(new PagedListResult
        {
            Items = items,
            Prefixes = [],
            NextPageToken = hasMore ? (startIndex + maxResults).ToString() : null
        }));
    }

    public async Task<Result<Stream>> GetStreamAsync(long maxSize = 10 * 1024 * 1024)
    {
        var bytesResult = await GetBytesAsync(maxSize);

        if (bytesResult.IsSuccess)
        {
            return Result<Stream>.Success(new MemoryStream(bytesResult.Value));
        }

        return Result<Stream>.Failure(bytesResult.Error!);
    }

    private static string NormalizePath(string path)
    {
        return path.TrimStart('/').TrimEnd('/');
    }
}
