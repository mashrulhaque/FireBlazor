namespace FireBlazor;

/// <summary>
/// Extension methods for IFirestore to support attribute-based collection resolution.
/// </summary>
public static class FirestoreExtensions
{
    /// <summary>
    /// Gets a collection reference using the collection path from the FirestoreCollectionAttribute.
    /// </summary>
    /// <typeparam name="T">The entity type decorated with FirestoreCollectionAttribute</typeparam>
    /// <param name="firestore">The Firestore instance</param>
    /// <returns>A collection reference for the entity type</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type does not have a FirestoreCollectionAttribute
    /// </exception>
    /// <example>
    /// <code>
    /// [FirestoreCollection("users")]
    /// public class User { ... }
    ///
    /// // Instead of: firebase.Firestore.Collection&lt;User&gt;("users")
    /// // Use: firebase.Firestore.Collection&lt;User&gt;()
    /// var users = firebase.Firestore.Collection&lt;User&gt;();
    /// </code>
    /// </example>
    public static ICollectionReference<T> Collection<T>(this IFirestore firestore) where T : class
    {
        var path = FirestoreCollectionAttribute.GetCollectionPath<T>();

        if (path is null)
        {
            throw new InvalidOperationException(
                $"Type '{typeof(T).Name}' does not have a FirestoreCollectionAttribute. " +
                $"Either add [FirestoreCollection(\"path\")] to the class or use Collection<T>(string path) instead.");
        }

        return firestore.Collection<T>(path);
    }
}
