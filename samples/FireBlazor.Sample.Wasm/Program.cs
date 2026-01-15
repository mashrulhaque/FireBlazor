using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FireBlazor;
using FireBlazor.Sample.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Load local credentials if available (firebase-credentials.local.json is gitignored)
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
try
{
    var localConfig = await http.GetStreamAsync("firebase-credentials.local.json");
    builder.Configuration.AddJsonStream(localConfig);
}
catch (HttpRequestException)
{
    // Local credentials file not found - using placeholders
    // Create firebase-credentials.local.json with your Firebase config for local development
}

// Read Firebase config from configuration (falls back to placeholders if not set)
var firebaseConfig = builder.Configuration.GetSection("Firebase");

// Configure Firebase
builder.Services.AddFirebase(options => options
    .WithProject(firebaseConfig["ProjectId"] ?? "your-project-id")
    .WithApiKey(firebaseConfig["ApiKey"] ?? "your-api-key")
    .WithAppId(firebaseConfig["AppId"] ?? "your-app-id")
    .WithAuthDomain(firebaseConfig["AuthDomain"] ?? "your-project.firebaseapp.com")
    .WithStorageBucket(firebaseConfig["StorageBucket"] ?? "your-project.firebasestorage.app")
    .WithDatabaseUrl(firebaseConfig["DatabaseUrl"] ?? "https://your-project.firebasedatabase.app")
    .UseAuth(auth => auth.EnableEmailPassword())
    .UseFirestore()
    .UseStorage()
    .UseRealtimeDatabase()
    .UseAppCheck(appCheck => appCheck
        .ReCaptchaV3(firebaseConfig["ReCaptchaSiteKey"] ?? "your-recaptcha-site-key"))
        // .AutoDebug() // Falls back to debug on localhost if needed
    // Uncomment for local development with Firebase emulators:
    // .UseEmulators(emulators => emulators
    //     .Auth("localhost:9099")
    //     .Firestore("localhost:8080")
    //     .Storage("localhost:9199")
    //     .RealtimeDatabase("localhost:9000"))
    .WithLogging(log =>
    {
        log.LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
        log.LogOperations = true;
    }));

// Add Blazor authorization integration with Firebase Auth
builder.Services.AddFirebaseAuthorization(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
});

await builder.Build().RunAsync();
