using System.Text.Json;

namespace FireBlazor.Tests.AILogic;

public class FunctionCallingTests
{
    [Fact]
    public void FunctionDeclaration_CreatesCorrectly()
    {
        // Act
        var declaration = new FunctionDeclaration
        {
            Name = "getWeather",
            Description = "Gets the current weather",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, ParameterSchema>
                {
                    ["location"] = new() { Type = "string", Description = "The city name" }
                },
                Required = new[] { "location" }
            }
        };

        // Assert
        Assert.Equal("getWeather", declaration.Name);
        Assert.NotNull(declaration.Parameters?.Properties);
        Assert.Contains("location", declaration.Parameters.Properties.Keys);
    }

    [Fact]
    public void FunctionCall_GetArgument_ReturnsValue()
    {
        // Arrange
        var json = JsonDocument.Parse("""{"city": "Seattle", "units": "metric"}""");
        var call = new FunctionCall
        {
            Name = "getWeather",
            Arguments = json.RootElement
        };

        // Act
        var city = call.GetArgument<string>("city");
        var units = call.GetArgument<string>("units");

        // Assert
        Assert.Equal("Seattle", city);
        Assert.Equal("metric", units);
    }

    [Fact]
    public void FunctionCall_TryGetArgument_ReturnsFalseForMissing()
    {
        // Arrange
        var json = JsonDocument.Parse("""{"city": "Seattle"}""");
        var call = new FunctionCall
        {
            Name = "getWeather",
            Arguments = json.RootElement
        };

        // Act
        var found = call.TryGetArgument<string>("country", out var country);

        // Assert
        Assert.False(found);
        Assert.Null(country);
    }

    [Fact]
    public void FunctionCall_GetAllArguments_ReturnsDict()
    {
        // Arrange
        var json = JsonDocument.Parse("""{"city": "Seattle", "temp": 72, "sunny": true}""");
        var call = new FunctionCall
        {
            Name = "getWeather",
            Arguments = json.RootElement
        };

        // Act
        var args = call.GetAllArguments();

        // Assert
        Assert.Equal("Seattle", args["city"]);
        Assert.Equal(72.0, args["temp"]);
        Assert.Equal(true, args["sunny"]);
    }

    [Fact]
    public void FunctionResponse_FromString_CreatesCorrectly()
    {
        // Act
        var response = FunctionResponse.FromString("getWeather", "Sunny, 72F");

        // Assert
        Assert.Equal("getWeather", response.Name);
        Assert.Equal("Sunny, 72F", response.Response);
    }

    [Fact]
    public void FunctionResponse_FromError_CreatesCorrectly()
    {
        // Act
        var response = FunctionResponse.FromError("getWeather", "City not found");

        // Assert
        Assert.Equal("getWeather", response.Name);
    }

    [Fact]
    public void Tool_WithFunctions_CreatesCorrectly()
    {
        // Arrange
        var func1 = new FunctionDeclaration { Name = "func1", Description = "First function" };
        var func2 = new FunctionDeclaration { Name = "func2", Description = "Second function" };

        // Act
        var tool = Tool.WithFunctions(func1, func2);

        // Assert
        Assert.NotNull(tool.FunctionDeclarations);
        Assert.Equal(2, tool.FunctionDeclarations.Count);
    }
}

public class FunctionRegistryTests
{
    public class TestFunctions
    {
        [AIFunction(Description = "Adds two numbers together")]
        public int Add(
            [AIParameter(Description = "First number")] int a,
            [AIParameter(Description = "Second number")] int b)
        {
            return a + b;
        }

        [AIFunction(Name = "greet", Description = "Greets a person")]
        public string Greet(
            [AIParameter(Description = "The person's name")] string name)
        {
            return $"Hello, {name}!";
        }

        [AIFunction(Description = "Gets data asynchronously")]
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(1);
            return "data";
        }
    }

    [Fact]
    public void RegisterInstance_DiscoversMarkedMethods()
    {
        // Arrange
        var registry = new FunctionRegistry();
        var instance = new TestFunctions();

        // Act
        registry.RegisterInstance(instance);
        var declarations = registry.GetDeclarations();

        // Assert
        Assert.Equal(3, declarations.Count);
        Assert.Contains(declarations, d => d.Name == "Add");
        Assert.Contains(declarations, d => d.Name == "greet");
        Assert.Contains(declarations, d => d.Name == "GetDataAsync");
    }

    [Fact]
    public void GetDeclarations_IncludesParameterInfo()
    {
        // Arrange
        var registry = new FunctionRegistry();
        registry.RegisterInstance(new TestFunctions());

        // Act
        var addDecl = registry.GetDeclarations().First(d => d.Name == "Add");

        // Assert
        Assert.NotNull(addDecl.Parameters?.Properties);
        Assert.Contains("a", addDecl.Parameters.Properties.Keys);
        Assert.Contains("b", addDecl.Parameters.Properties.Keys);
        Assert.Equal("number", addDecl.Parameters.Properties["a"].Type);
    }

    [Fact]
    public async Task ExecuteAsync_CallsMethod()
    {
        // Arrange
        var registry = new FunctionRegistry();
        registry.RegisterInstance(new TestFunctions());

        var json = JsonDocument.Parse("""{"a": 5, "b": 3}""");
        var call = new FunctionCall { Name = "Add", Arguments = json.RootElement };

        // Act
        var response = await registry.ExecuteAsync(call);

        // Assert
        Assert.Equal("Add", response.Name);
        Assert.Equal(8, response.Response);
    }

    [Fact]
    public async Task ExecuteAsync_CallsAsyncMethod()
    {
        // Arrange
        var registry = new FunctionRegistry();
        registry.RegisterInstance(new TestFunctions());

        var json = JsonDocument.Parse("{}");
        var call = new FunctionCall { Name = "GetDataAsync", Arguments = json.RootElement };

        // Act
        var response = await registry.ExecuteAsync(call);

        // Assert
        Assert.Equal("GetDataAsync", response.Name);
        Assert.Equal("data", response.Response);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownFunction_ReturnsError()
    {
        // Arrange
        var registry = new FunctionRegistry();
        var json = JsonDocument.Parse("{}");
        var call = new FunctionCall { Name = "Unknown", Arguments = json.RootElement };

        // Act
        var response = await registry.ExecuteAsync(call);

        // Assert
        Assert.Equal("Unknown", response.Name);
    }
}
