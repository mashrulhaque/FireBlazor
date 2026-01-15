using System.Reflection;

namespace FireBlazor;

/// <summary>
/// Specifies the Firestore collection path for an entity type.
/// Apply this attribute to classes that represent Firestore documents.
/// </summary>
/// <example>
/// <code>
/// [FirestoreCollection("users")]
/// public class User
/// {
///     public string? Id { get; set; }
///     public string? Name { get; set; }
/// }
///
/// // Then use: firebase.Firestore.Collection&lt;User&gt;()
/// // which automatically resolves to "users" collection
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class FirestoreCollectionAttribute : Attribute
{
    /// <summary>
    /// The Firestore collection path for this entity type.
    /// </summary>
    public string CollectionPath { get; }

    /// <summary>
    /// Creates a new FirestoreCollectionAttribute with the specified collection path.
    /// </summary>
    /// <param name="collectionPath">The Firestore collection path (e.g., "users" or "users/{userId}/posts")</param>
    public FirestoreCollectionAttribute(string collectionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionPath);
        CollectionPath = collectionPath;
    }

    /// <summary>
    /// Gets the collection path for a type, if the FirestoreCollectionAttribute is applied.
    /// </summary>
    /// <typeparam name="T">The entity type to check</typeparam>
    /// <returns>The collection path, or null if no attribute is found</returns>
    public static string? GetCollectionPath<T>() where T : class
    {
        return GetCollectionPath(typeof(T));
    }

    /// <summary>
    /// Gets the collection path for a type, if the FirestoreCollectionAttribute is applied.
    /// </summary>
    /// <param name="type">The entity type to check</param>
    /// <returns>The collection path, or null if no attribute is found</returns>
    public static string? GetCollectionPath(Type type)
    {
        var attr = type.GetCustomAttribute<FirestoreCollectionAttribute>();
        return attr?.CollectionPath;
    }
}
