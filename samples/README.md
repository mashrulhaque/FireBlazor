# FireBlazor Sample Applications

This directory contains sample applications demonstrating how to use FireBlazor with Blazor WebAssembly.

## Projects

- **FireBlazor.Sample.Wasm** - Blazor WebAssembly sample (runs entirely in the browser)

## Prerequisites

1. .NET 9.0 SDK or later
2. A Firebase project (create one at [Firebase Console](https://console.firebase.google.com))

## Firebase Setup

### 1. Create a Firebase Project

1. Go to the [Firebase Console](https://console.firebase.google.com)
2. Click "Add project" and follow the setup wizard
3. Once created, click the gear icon > Project settings

### 2. Register a Web App

1. In Project settings, scroll to "Your apps"
2. Click the Web icon (`</>`) to add a web app
3. Copy the Firebase configuration values

### 3. Enable Firebase Services

Enable the services you want to use:

- **Authentication**: Authentication > Sign-in method > Enable Email/Password, Google, GitHub, etc.
- **Cloud Firestore**: Firestore Database > Create database > Start in test mode
- **Cloud Storage**: Storage > Get started
- **Realtime Database**: Realtime Database > Create database

### 4. Configure the Sample

Update `Program.cs` with your Firebase project configuration:

```csharp
builder.Services.AddFirebase(options => options
    .WithProject("your-project-id")
    .WithApiKey("your-api-key")
    .WithAuthDomain("your-project.firebaseapp.com")
    .WithStorageBucket("your-project.appspot.com")
    .WithDatabaseUrl("https://your-project.firebaseio.com")
    .UseAuth()
    .UseFirestore()
    .UseStorage()
    .UseRealtimeDatabase());
```

## Running the Sample

```bash
cd samples/FireBlazor.Sample.Wasm
dotnet run
```

Navigate to `https://localhost:7098` (or the URL shown in terminal).

## Sample Pages

The sample includes the following demonstration pages:

| Page | Route | Description |
|------|-------|-------------|
| Home | `/` | Overview and getting started guide |
| Login | `/login` | Authentication demo (Email, Google, GitHub) |
| CRUD | `/crud` | Firestore create, read, update, delete operations |
| File Upload | `/upload` | Cloud Storage file upload with progress |
| Realtime DB | `/realtime` | Real-time database messaging demo |

## Using Firebase Emulators (Optional)

For local development without a Firebase project, use the [Firebase Emulator Suite](https://firebase.google.com/docs/emulator-suite).

### Setup Emulators

1. Install Firebase CLI: `npm install -g firebase-tools`
2. Initialize: `firebase init emulators`
3. Start emulators: `firebase emulators:start`

### Configure Sample for Emulators

Uncomment the emulator configuration in `Program.cs`:

```csharp
.UseEmulators(emulators => emulators
    .Auth("localhost:9099")
    .Firestore("localhost:8080")
    .Storage("localhost:9199")
    .RealtimeDatabase("localhost:9000"))
```

## Features Demonstrated

### Authentication
- Email/password sign-in and registration
- Google OAuth popup sign-in
- GitHub OAuth popup sign-in
- Auth state change listeners
- Sign out functionality

### Cloud Firestore
- Add documents to a collection
- Query documents with real-time updates
- Update document fields
- Delete documents
- Error handling with Result pattern

### Cloud Storage
- File selection with validation
- Upload with progress tracking
- File listing with download URLs
- File deletion
- Max file size enforcement (10MB)

### Realtime Database
- Real-time data listeners (OnValue)
- Push new data entries
- Query with OrderByChild and LimitToLast
- Delete entries
- Connection status display

## Security Notes

- **API Keys are public**: Firebase web API keys are designed to be public. Security is enforced via Firebase Security Rules.
- **Configure Security Rules**: Set up proper [Security Rules](https://firebase.google.com/docs/rules) for Firestore, Storage, and Realtime Database before deploying.
- **Enable App Check**: Consider enabling [Firebase App Check](https://firebase.google.com/docs/app-check) for production apps.

## Troubleshooting

### "Firebase not initialized" error
Ensure `await Firebase.InitializeAsync()` is called before using Firebase services (typically in `OnInitializedAsync`).

### OAuth popup blocked
Browser popup blockers may prevent OAuth sign-in. Allow popups for localhost during development.

### Firestore permission denied
Check your Firestore Security Rules. For testing, you can use:
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if true;
    }
  }
}
```
**Warning**: This allows unrestricted access. Use proper rules in production.
