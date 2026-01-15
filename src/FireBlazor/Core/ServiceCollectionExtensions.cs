using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FireBlazor;

/// <summary>
/// Extension methods for configuring Firebase services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Firebase services to the service collection with fluent configuration.
    /// </summary>
    public static IServiceCollection AddFirebase(
        this IServiceCollection services,
        Action<FirebaseOptions> configure)
    {
        var options = new FirebaseOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<IFirebase>(sp =>
        {
            var opts = sp.GetRequiredService<FirebaseOptions>();
            var logger = sp.GetRequiredService<ILogger<Firebase>>();
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            return new Firebase(opts, logger, jsRuntime);
        });

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        return services;
    }

    /// <summary>
    /// Adds Firebase services with configuration from IConfiguration and fluent overrides.
    /// </summary>
    public static IServiceCollection AddFirebase(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FirebaseOptions>? configure = null)
    {
        var options = new FirebaseOptions();

        // Bind from configuration
        var section = configuration.GetSection("Firebase");
        if (section.Exists())
        {
            options.WithProject(section["ProjectId"] ?? "")
                   .WithApiKey(section["ApiKey"] ?? "")
                   .WithAuthDomain(section["AuthDomain"] ?? "")
                   .WithStorageBucket(section["StorageBucket"] ?? "")
                   .WithDatabaseUrl(section["DatabaseUrl"] ?? "");
        }

        // Apply fluent overrides
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IFirebase>(sp =>
        {
            var opts = sp.GetRequiredService<FirebaseOptions>();
            var logger = sp.GetRequiredService<ILogger<Firebase>>();
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            return new Firebase(opts, logger, jsRuntime);
        });

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        return services;
    }

    /// <summary>
    /// Adds Firebase authorization integration with Blazor's authorization system.
    /// Enables [Authorize] attributes, AuthorizeView components, and role-based access control.
    /// </summary>
    public static IServiceCollection AddFirebaseAuthorization(
        this IServiceCollection services,
        Action<FirebaseAuthorizationOptions>? configure = null)
    {
        var options = new FirebaseAuthorizationOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<AuthenticationStateProvider>(sp =>
        {
            var auth = sp.GetRequiredService<IFirebase>().Auth;
            var opts = sp.GetRequiredService<FirebaseAuthorizationOptions>();
            var logger = sp.GetRequiredService<ILogger<FirebaseAuthenticationStateProvider>>();
            return new FirebaseAuthenticationStateProvider(auth, opts, logger);
        });
        services.AddAuthorizationCore();

        return services;
    }
}

/// <summary>
/// Null logger implementation for when no logging is configured.
/// </summary>
internal sealed class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
