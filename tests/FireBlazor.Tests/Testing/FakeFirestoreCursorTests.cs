using FireBlazor.Testing;

namespace FireBlazor.Tests.Testing;

public class FakeFirestoreCursorTests
{
    private readonly FakeFirestore _firestore = new();

    public class Product
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int Price { get; set; }
    }

    [Fact]
    public async Task StartAt_IncludesDocumentsFromCursor()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Price)
            .StartAt(200)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("B", result.Value[0].Data!.Name);
        Assert.Equal("C", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task StartAfter_SkipsDocumentsBeforeCursor()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Price)
            .StartAfter(100)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("B", result.Value[0].Data!.Name);
        Assert.Equal("C", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task EndAt_IncludesDocumentsUpToCursor()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Price)
            .EndAt(200)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("A", result.Value[0].Data!.Name);
        Assert.Equal("B", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task EndBefore_StopsBeforeCursor()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Price)
            .EndBefore(300)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("A", result.Value[0].Data!.Name);
        Assert.Equal("B", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task Cursors_CanBeCombined()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });
        await collection.Document("4").SetAsync(new Product { Id = "4", Name = "D", Price = 400 });

        // Act - get only B and C (after 100, before 400)
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Price)
            .StartAfter(100)
            .EndBefore(400)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("B", result.Value[0].Data!.Name);
        Assert.Equal("C", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task StartAt_WithDescendingOrder_WorksCorrectly()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderByDescending(p => p.Price)
            .StartAt(200)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("B", result.Value[0].Data!.Name);
        Assert.Equal("A", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task Cursors_WorkWithStringFields()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "Apple", Price = 100 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "Banana", Price = 200 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "Cherry", Price = 300 });

        // Act
        var result = await _firestore.Collection<Product>("products")
            .OrderBy(p => p.Name)
            .StartAfter("Apple")
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Banana", result.Value[0].Data!.Name);
        Assert.Equal("Cherry", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task Cursors_ChainWithWhereAndTake()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 50 });
        await collection.Document("2").SetAsync(new Product { Id = "2", Name = "B", Price = 100 });
        await collection.Document("3").SetAsync(new Product { Id = "3", Name = "C", Price = 150 });
        await collection.Document("4").SetAsync(new Product { Id = "4", Name = "D", Price = 200 });
        await collection.Document("5").SetAsync(new Product { Id = "5", Name = "E", Price = 250 });

        // Act - products with price > 75, starting after 100, limit 2
        var result = await _firestore.Collection<Product>("products")
            .Where(p => p.Price > 75)
            .OrderBy(p => p.Price)
            .StartAfter(100)
            .Take(2)
            .GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("C", result.Value[0].Data!.Name);
        Assert.Equal("D", result.Value[1].Data!.Name);
    }

    [Fact]
    public async Task GetAsync_WithCursorsButNoOrderBy_ThrowsInvalidOperationException()
    {
        // Arrange
        var collection = _firestore.Collection<Product>("products");
        await collection.Document("1").SetAsync(new Product { Id = "1", Name = "A", Price = 100 });

        // Act & Assert
        var query = _firestore.Collection<Product>("products").StartAfter(50);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => query.GetAsync());

        Assert.Contains("OrderBy", exception.Message);
    }
}
