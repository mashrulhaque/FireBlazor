namespace FireBlazor;

/// <summary>
/// Firebase AI Logic service interface for accessing generative AI models.
/// </summary>
public interface IFirebaseAI
{
    /// <summary>
    /// Gets a generative model instance with the specified model name and configuration.
    /// </summary>
    /// <param name="modelName">The name of the model to use (e.g., "gemini-2.0-flash").</param>
    /// <param name="config">Optional configuration for content generation.</param>
    /// <returns>A generative model instance.</returns>
    IGenerativeModel GetModel(string modelName, GenerationConfig? config = null);

    /// <summary>
    /// Gets an image generation model (Imagen).
    /// </summary>
    /// <param name="modelName">Model name. Default: "imagen-4.0-generate-001"</param>
    /// <returns>An IImageModel instance for generating images.</returns>
    IImageModel GetImageModel(string modelName = "imagen-4.0-generate-001");
}
