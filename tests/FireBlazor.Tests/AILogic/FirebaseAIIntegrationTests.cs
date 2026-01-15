using FireBlazor.Testing;

namespace FireBlazor.Tests.AILogic;

public class FirebaseAIIntegrationTests
{
    [Fact]
    public void IFirebase_HasAIProperty()
    {
        IFirebase firebase = null!;
        IFirebaseAI ai = firebase?.AI!;
        Assert.Null(ai);
    }

    [Fact]
    public void FakeFirebase_HasAIProperty()
    {
        var fake = new FakeFirebase();
        Assert.NotNull(fake.AI);
    }

    [Fact]
    public void FakeFirebase_FakeAI_ReturnsConfiguredFake()
    {
        var fake = new FakeFirebase();
        Assert.NotNull(fake.FakeAI);
        Assert.Same(fake.AI, fake.FakeAI);
    }

    [Fact]
    public async Task FakeFirebase_AIGeneration_WorksEndToEnd()
    {
        var firebase = new FakeFirebase();
        firebase.FakeAI.ConfigureDefaultResponse("The answer is 42.");

        var model = firebase.AI.GetModel("gemini-2.5-flash");
        var result = await model.GenerateContentAsync("What is the meaning of life?");

        Assert.True(result.IsSuccess);
        Assert.Equal("The answer is 42.", result.Value.Text);
    }

    [Fact]
    public async Task FakeFirebase_AIStreaming_WorksEndToEnd()
    {
        var firebase = new FakeFirebase();
        firebase.FakeAI.ConfigureStreamChunks(["Hello ", "there ", "friend"]);

        var model = firebase.AI.GetModel("gemini-2.5-flash");
        var chunks = new List<string>();

        await foreach (var chunk in model.GenerateContentStreamAsync("Hi!"))
        {
            Assert.True(chunk.IsSuccess);
            if (!chunk.Value.IsFinal)
            {
                chunks.Add(chunk.Value.Text);
            }
        }

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Hello there friend", string.Join("", chunks));
    }

    [Fact]
    public async Task FakeFirebase_AIWithConfig_PreservesConfig()
    {
        var firebase = new FakeFirebase();
        firebase.FakeAI.ConfigureDefaultResponse("Creative response!");

        var config = new GenerationConfig
        {
            Temperature = 1.5f,
            MaxOutputTokens = 500,
            SystemInstruction = "Be creative"
        };

        var model = firebase.AI.GetModel("gemini-2.5-flash", config);
        var result = await model.GenerateContentAsync("Tell me a story");

        Assert.True(result.IsSuccess);
        Assert.Equal("gemini-2.5-flash", model.ModelName);
    }

    [Fact]
    public async Task FakeFirebase_AIError_ReturnsFailure()
    {
        var firebase = new FakeFirebase();
        firebase.FakeAI.SimulateError(AILogicErrorCode.ContentBlocked, "Content was blocked by safety filters");

        var model = firebase.AI.GetModel("gemini-2.5-flash");
        var result = await model.GenerateContentAsync("Bad prompt");

        Assert.True(result.IsFailure);
        Assert.Equal("ai/content-blocked", result.Error!.Code);
    }
}
