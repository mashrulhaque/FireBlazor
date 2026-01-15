namespace FireBlazor.Platform.Wasm;

/// <summary>
/// WebAssembly implementation of IFirebaseStorage using JavaScript interop.
/// </summary>
internal sealed class WasmFirebaseStorage : IFirebaseStorage
{
    private readonly FirebaseJsInterop _jsInterop;
    private readonly long _maxBrowserFileSize;

    public WasmFirebaseStorage(FirebaseJsInterop jsInterop, StorageOptions? options = null)
    {
        _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
        _maxBrowserFileSize = options?.MaxBrowserFileSize ?? StorageOptions.DefaultMaxBrowserFileSize;
    }

    public IStorageReference Ref(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new WasmStorageReference(_jsInterop, path, _maxBrowserFileSize);
    }
}
