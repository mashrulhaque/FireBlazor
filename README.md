# FireBlazor

**Firebase for Blazor - Zero boilerplate, delightful DX**

A comprehensive, type-safe Firebase SDK for Blazor WebAssembly applications. FireBlazor provides first-class .NET integration with Firebase services including Authentication, Cloud Firestore, Cloud Storage, Realtime Database, App Check, and Firebase AI Logic (Gemini).

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/FireBlazor.svg)](https://www.nuget.org/packages/FireBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/user/FireBlazor?style=social)](https://github.com/user/FireBlazor)

---

## Why FireBlazor?

Looking for a **Firebase SDK for Blazor** or a **.NET Firebase alternative**? The official Firebase JavaScript SDK doesn't integrate well with Blazor's component model. FireBlazor solves this with:

| FireBlazor | JS SDK via Interop |
|------------|-------------------|
| Native C# types | Manual JS interop |
| LINQ-style queries | String-based queries |
| `Result<T>` error handling | Try-catch exceptions |
| Blazor authorization integration | Manual auth state |
| IntelliSense & compile-time safety | Runtime errors |
| Full emulator support | Complex setup |

**Perfect for:** Blazor WebAssembly apps needing Firebase Authentication, Firestore, Storage, Realtime Database, or Gemini AI.

---

## Demo

<!-- TODO: Add screenshot or GIF of sample app -->
<!-- ![FireBlazor Demo](docs/images/demo.gif) -->

Check out the [sample application](samples/FireBlazor.Sample.Wasm) to see FireBlazor in action.

---

## Features

- **Firebase Authentication** - Email/password, Google, GitHub, Microsoft OAuth
- **Cloud Firestore** - LINQ-style queries, real-time subscriptions, transactions, batch operations
- **Cloud Storage** - File upload/download with progress tracking, metadata management
- **Realtime Database** - Real-time synchronization, presence detection, offline support
- **App Check** - reCAPTCHA v3/Enterprise integration for backend protection
- **Firebase AI Logic** - Gemini model integration with streaming, function calling, and grounding
- **Result Pattern** - Functional error handling without exceptions
- **Blazor Authorization** - Seamless integration with `[Authorize]` and `<AuthorizeView>`
- **Emulator Support** - Full local development with Firebase Emulator Suite
- **Testing Infrastructure** - Complete fake implementations for unit testing

## Installation

```bash
dotnet add package FireBlazor
```

## Getting Started

### 1. Configure Services

```csharp
// Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddFirebase(options => options
    .WithProject("your-project-id")
    .WithApiKey("your-api-key")
    .WithAuthDomain("your-project.firebaseapp.com")
    .WithStorageBucket("your-project.firebasestorage.app")
    .WithDatabaseUrl("https://your-project.firebasedatabase.app")
    .UseAuth(auth => auth
        .EnableEmailPassword()
        .EnableGoogle("google-client-id"))
    .UseFirestore()
    .UseStorage()
    .UseRealtimeDatabase()
    .UseAppCheck(appCheck => appCheck
        .ReCaptchaV3("your-recaptcha-site-key")));

// Optional: Enable Blazor authorization
builder.Services.AddFirebaseAuthorization(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
});
```

### 2. Initialize Firebase

```razor
@inject IFirebase Firebase

@code {
    protected override async Task OnInitializedAsync()
    {
        await Firebase.InitializeAsync();
    }
}
```

### 3. Use Firebase Services

```razor
@inject IFirebase Firebase

<h3>Welcome, @_user?.DisplayName</h3>

@code {
    private FirebaseUser? _user;

    protected override void OnInitialized()
    {
        _user = Firebase.Auth.CurrentUser;
        Firebase.Auth.OnAuthStateChanged += user =>
        {
            _user = user;
            InvokeAsync(StateHasChanged);
        };
    }
}
```

## Authentication

FireBlazor supports multiple authentication providers:

```csharp
// Email/Password
var result = await Firebase.Auth.SignInWithEmailAsync("user@example.com", "password");
var result = await Firebase.Auth.CreateUserWithEmailAsync("user@example.com", "password");

// OAuth Providers
var result = await Firebase.Auth.SignInWithGoogleAsync();
var result = await Firebase.Auth.SignInWithGitHubAsync();
var result = await Firebase.Auth.SignInWithMicrosoftAsync();

// Sign Out
await Firebase.Auth.SignOutAsync();

// Get ID Token
var tokenResult = await Firebase.Auth.GetIdTokenAsync(forceRefresh: true);
```

### User Properties

```csharp
FirebaseUser user = Firebase.Auth.CurrentUser;
// user.Uid, user.Email, user.DisplayName, user.PhotoUrl
// user.IsEmailVerified, user.IsAnonymous, user.Providers
// user.CreatedAt, user.LastSignInAt
```

## Cloud Firestore

### CRUD Operations

```csharp
// Create
var result = await Firebase.Firestore
    .Collection<TodoItem>("todos")
    .AddAsync(new TodoItem { Title = "Buy groceries", Completed = false });

// Read
var result = await Firebase.Firestore
    .Collection<TodoItem>("todos")
    .Where(t => t.Completed == false)
    .OrderBy(t => t.CreatedAt)
    .Take(10)
    .GetAsync();

// Update
var result = await Firebase.Firestore
    .Collection<TodoItem>("todos")
    .Document("doc-id")
    .UpdateAsync(new { Completed = true });

// Delete
var result = await Firebase.Firestore
    .Collection<TodoItem>("todos")
    .Document("doc-id")
    .DeleteAsync();
```

### Real-time Subscriptions

```csharp
var unsubscribe = Firebase.Firestore
    .Collection<TodoItem>("todos")
    .Where(t => t.UserId == currentUserId)
    .OnSnapshot(
        snapshot => {
            _items = snapshot.ToList();
            InvokeAsync(StateHasChanged);
        },
        error => Console.WriteLine(error.Message));

// Later: unsubscribe();
```

### Transactions & Batch Operations

```csharp
// Transaction
var result = await Firebase.Firestore.TransactionAsync<string>(async tx =>
{
    var fromDoc = Firebase.Firestore.Collection<Account>("accounts").Document("from");
    var toDoc = Firebase.Firestore.Collection<Account>("accounts").Document("to");

    var fromSnap = await tx.GetAsync(fromDoc);
    var toSnap = await tx.GetAsync(toDoc);

    tx.Update(fromDoc, new { Balance = fromSnap.Value.Data.Balance - 100 });
    tx.Update(toDoc, new { Balance = toSnap.Value.Data.Balance + 100 });

    return "Transfer complete";
});

// Batch
var result = await Firebase.Firestore.BatchAsync(batch =>
{
    batch.Set(collection.Document("doc1"), item1);
    batch.Update(collection.Document("doc2"), new { Status = "updated" });
    batch.Delete(collection.Document("doc3"));
});
```

### Field Values

```csharp
// Server timestamp
await docRef.UpdateAsync(new { UpdatedAt = FieldValue.ServerTimestamp() });

// Atomic increment
await docRef.UpdateAsync(new { ViewCount = FieldValue.Increment(1) });

// Array operations
await docRef.UpdateAsync(new { Tags = FieldValue.ArrayUnion("new-tag") });
await docRef.UpdateAsync(new { Tags = FieldValue.ArrayRemove("old-tag") });
```

### Aggregate Queries

```csharp
var count = await Firebase.Firestore.Collection<Item>("items").Aggregate().CountAsync();
var total = await Firebase.Firestore.Collection<Order>("orders").Aggregate().SumAsync("amount");
var avg = await Firebase.Firestore.Collection<Product>("products").Aggregate().AverageAsync("price");
```

## Cloud Storage

### Upload Files

```csharp
// Upload with progress
var result = await Firebase.Storage
    .Ref($"uploads/{Guid.NewGuid()}/{file.Name}")
    .PutAsync(
        browserFile,
        new StorageMetadata { ContentType = file.ContentType },
        progress => {
            _uploadProgress = progress.Percentage;
            InvokeAsync(StateHasChanged);
        });

// Upload string content
await Firebase.Storage
    .Ref("data/config.json")
    .PutStringAsync(jsonContent, StringFormat.Raw, new StorageMetadata { ContentType = "application/json" });
```

### Download Files

```csharp
// Get download URL
var urlResult = await Firebase.Storage.Ref("path/to/file.png").GetDownloadUrlAsync();

// Download as bytes
var bytesResult = await Firebase.Storage.Ref("path/to/file.png").GetBytesAsync(maxSize: 10_000_000);

// Download as stream
var streamResult = await Firebase.Storage.Ref("path/to/file.png").GetStreamAsync();
```

### Manage Files

```csharp
// Get metadata
var metadata = await Firebase.Storage.Ref("path/to/file").GetMetadataAsync();

// Update metadata
await Firebase.Storage.Ref("path/to/file").UpdateMetadataAsync(new StorageMetadata
{
    CacheControl = "public, max-age=3600",
    CustomMetadata = new Dictionary<string, string> { ["version"] = "1.0" }
});

// Delete
await Firebase.Storage.Ref("path/to/file").DeleteAsync();

// List files
var listResult = await Firebase.Storage.Ref("uploads").ListAllAsync();
```

## Realtime Database

### Basic Operations

```csharp
// Set data
await Firebase.RealtimeDb.Ref("users/user1").SetAsync(userData);

// Push data (auto-generated key)
var pushResult = await Firebase.RealtimeDb.Ref("messages").PushAsync(message);

// Get data
var result = await Firebase.RealtimeDb.Ref("users/user1").GetAsync<User>();

// Update data
await Firebase.RealtimeDb.Ref("users/user1").UpdateAsync(new { Status = "online" });

// Remove data
await Firebase.RealtimeDb.Ref("users/user1").RemoveAsync();
```

### Real-time Listeners

```csharp
var unsubscribe = Firebase.RealtimeDb
    .Ref("messages")
    .OrderByChild("timestamp")
    .LimitToLast(50)
    .OnValue<Dictionary<string, Message>>(
        snapshot => {
            if (snapshot.Exists)
                _messages = snapshot.Value.Values.ToList();
            InvokeAsync(StateHasChanged);
        },
        error => Console.WriteLine(error.Message));
```

### Server Values & Transactions

```csharp
// Server timestamp
await Firebase.RealtimeDb.Ref("posts/post1").UpdateAsync(new {
    CreatedAt = ServerValue.Timestamp
});

// Atomic increment
await Firebase.RealtimeDb.Ref("counters/visits").SetAsync(ServerValue.Increment(1));

// Transaction
var result = await Firebase.RealtimeDb
    .Ref("likes/post1")
    .TransactionAsync<int>(current => (current ?? 0) + 1);
```

### Presence Detection

```csharp
// Set online status
await Firebase.RealtimeDb.Ref($"presence/{userId}").SetAsync(new { Status = "online" });

// Set offline status on disconnect
await Firebase.RealtimeDb
    .Ref($"presence/{userId}")
    .OnDisconnect()
    .SetAsync(new { Status = "offline", LastSeen = ServerValue.Timestamp });
```

## Firebase AI Logic (Gemini)

### Text Generation

```csharp
var model = Firebase.AI.GetModel("gemini-2.5-flash", new GenerationConfig
{
    SystemInstruction = "You are a helpful assistant.",
    Temperature = 0.7f,
    MaxOutputTokens = 1024
});

var result = await model.GenerateContentAsync("Explain quantum computing in simple terms.");
Console.WriteLine(result.Value.Text);
```

### Streaming Responses

```csharp
await foreach (var chunk in model.GenerateContentStreamAsync("Write a story about a robot."))
{
    if (chunk.IsSuccess && !chunk.Value.IsFinal)
    {
        _response += chunk.Value.Text;
        StateHasChanged();
    }
}
```

### Multi-turn Chat

```csharp
var chat = model.StartChat(new ChatOptions
{
    History = previousMessages
});

var response = await chat.SendMessageAsync("What's the weather like?");

// Streaming chat
await foreach (var chunk in chat.SendMessageStreamAsync("Tell me more."))
{
    _response += chunk.Value.Text;
    StateHasChanged();
}
```

### Multimodal Input

```csharp
var parts = new List<ContentPart>
{
    ContentPart.Text("What's in this image?"),
    ContentPart.Image(imageBytes, "image/png")
};

var result = await model.GenerateContentAsync(parts);
```

### Function Calling

```csharp
var config = new GenerationConfig
{
    Tools = new[]
    {
        new FunctionDeclaration
        {
            Name = "get_weather",
            Description = "Get weather for a location",
            Parameters = new { location = "string" }
        }
    }
};

var model = Firebase.AI.GetModel("gemini-2.5-flash", config);
var result = await model.GenerateContentAsync("What's the weather in Tokyo?");

if (result.Value.HasFunctionCalls)
{
    foreach (var call in result.Value.FunctionCalls)
    {
        // Handle function call
    }
}
```

### Grounding with Google Search

```csharp
var config = new GenerationConfig
{
    Grounding = GroundingConfig.WithGoogleSearch()
};

var model = Firebase.AI.GetModel("gemini-2.5-flash", config);
var result = await model.GenerateContentAsync("What are the latest news about AI?");

if (result.Value.IsGrounded)
{
    // Response includes web search results
}
```

### Image Generation

```csharp
var imageModel = Firebase.AI.GetImageModel("imagen-4.0-generate-001");
var result = await imageModel.GenerateImagesAsync(
    "A serene mountain landscape at sunset",
    new ImageGenerationConfig { NumberOfImages = 1 });
```

## App Check

```csharp
// Get App Check token
var tokenResult = await Firebase.AppCheck.GetTokenAsync();

// Monitor token changes
Firebase.AppCheck.OnTokenChanged += token =>
{
    Console.WriteLine($"New token expires at: {token?.ExpirationTime}");
};

// Check status
if (Firebase.AppCheck.IsActivated)
{
    Console.WriteLine($"Status: {Firebase.AppCheck.Status}");
}
```

## Error Handling

FireBlazor uses a `Result<T>` pattern for error handling:

```csharp
var result = await Firebase.Auth.SignInWithEmailAsync(email, password);

// Pattern matching
var message = result.Match(
    onSuccess: user => $"Welcome, {user.DisplayName}!",
    onFailure: error => $"Error: {error.Message}"
);

// Imperative style
if (result.IsSuccess)
{
    var user = result.Value;
}
else
{
    var error = result.Error;
    Console.WriteLine($"[{error.Code}] {error.Message}");
}

// Exception style (throws FirebaseException on failure)
var user = await Firebase.Auth.SignInWithEmailAsync(email, password).OrThrow();
```

## Emulator Support

For local development with Firebase Emulator Suite:

```csharp
builder.Services.AddFirebase(options => options
    .WithProject("demo-project")
    // ... other config
    .UseEmulators(emulators => emulators
        .Auth("localhost:9099")
        .Firestore("localhost:8080")
        .Storage("localhost:9199")
        .RealtimeDatabase("localhost:9000")));

// Or configure all at once with default ports
.UseEmulators(emulators => emulators.All("localhost"))
```

## Testing

FireBlazor provides fake implementations for unit testing:

```csharp
// Register fake services
services.AddSingleton<IFirebase, FakeFirebase>();

// In tests
var fakeFirebase = serviceProvider.GetRequiredService<IFirebase>() as FakeFirebase;

// Configure fake auth
fakeFirebase.FakeAuth.SetCurrentUser(new FirebaseUser { Uid = "test-user" });

// Reset between tests
fakeFirebase.Reset();
```

## Configuration from appsettings.json

```json
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ApiKey": "your-api-key",
    "AuthDomain": "your-project.firebaseapp.com",
    "StorageBucket": "your-project.firebasestorage.app",
    "DatabaseUrl": "https://your-project.firebasedatabase.app"
  }
}
```

```csharp
builder.Services.AddFirebase(configuration, options => options
    .UseAuth()
    .UseFirestore());
```

## Base Component for Subscriptions

Use `FirebaseComponentBase` for automatic subscription cleanup:

```csharp
@inherits FirebaseComponentBase

@code {
    protected override async Task OnInitializedAsync()
    {
        var unsubscribe = Firebase.Firestore
            .Collection<Item>("items")
            .OnSnapshot(snapshot => { /* handle */ });

        Subscribe(unsubscribe); // Automatically cleaned up on dispose
    }
}
```

## FAQ

### Does FireBlazor work with Blazor Server?

Currently, FireBlazor is designed for **Blazor WebAssembly** only. Blazor Server support is being evaluated for future releases.

### How does FireBlazor compare to the official Firebase JS SDK?

FireBlazor wraps the Firebase JS SDK via JavaScript interop but provides a native C# API with strong typing, LINQ support, and Blazor-specific integrations like authorization.

### Can I use FireBlazor with existing Firebase projects?

Yes! FireBlazor works with any existing Firebase project. Just configure your Firebase credentials and start using the services.

### Is FireBlazor production-ready?

FireBlazor is actively maintained and used in production applications. Check the [changelog](CHANGELOG.md) for the latest updates and version history.

### How do I report bugs or request features?

Please use [GitHub Issues](https://github.com/user/FireBlazor/issues) for bug reports and feature requests.

## Requirements

- .NET 9.0 or later
- Blazor WebAssembly
- Firebase project with desired services enabled

## Supported Firebase Services

| Service | Status |
|---------|--------|
| Authentication | Full support |
| Cloud Firestore | Full support |
| Cloud Storage | Full support |
| Realtime Database | Full support |
| App Check | Full support |
| Firebase AI Logic | Full support |
| Cloud Messaging | Planned |
| Remote Config | Planned |

## Community

- [GitHub Discussions](https://github.com/user/FireBlazor/discussions) - Ask questions and share ideas
- [GitHub Issues](https://github.com/user/FireBlazor/issues) - Report bugs and request features
- [Changelog](CHANGELOG.md) - See what's new

## Support the Project

If FireBlazor helps your project, please consider:

- Giving it a star on GitHub
- Sharing it with other Blazor developers
- [Contributing](CONTRIBUTING.md) to the project

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please read our [contributing guidelines](CONTRIBUTING.md) before submitting PRs.

---

Built with love for Blazor developers who want Firebase superpowers.
