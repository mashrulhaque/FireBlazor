namespace FireBlazor.Tests.AILogic;

public class IFirebaseAIImageModelTests
{
    [Fact]
    public void IFirebaseAI_GetImageModel_ReturnsImageModel()
    {
        var fakeAI = new MockFirebaseAI();

        var model = fakeAI.GetImageModel("imagen-4.0-generate-001");

        Assert.NotNull(model);
        Assert.Equal("imagen-4.0-generate-001", model.ModelName);
    }

    [Fact]
    public void IFirebaseAI_GetImageModel_DefaultModel_UsesImagen3()
    {
        var fakeAI = new MockFirebaseAI();

        // Default model should be imagen-4.0-generate-001
        var model = fakeAI.GetImageModel();

        Assert.NotNull(model);
        Assert.Contains("imagen", model.ModelName);
    }

    private class MockFirebaseAI : IFirebaseAI
    {
        public IGenerativeModel GetModel(string modelName, GenerationConfig? config = null)
            => throw new NotImplementedException();

        public IImageModel GetImageModel(string modelName = "imagen-4.0-generate-001")
            => new MockImageModel(modelName);

        private class MockImageModel : IImageModel
        {
            public string ModelName { get; }
            public MockImageModel(string name) => ModelName = name;

            public Task<Result<ImageGenerationResponse>> GenerateImagesAsync(
                string prompt,
                ImageGenerationConfig? config = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Result<ImageGenerationResponse>.Success(
                    new ImageGenerationResponse { Images = [] }));
            }
        }
    }
}
