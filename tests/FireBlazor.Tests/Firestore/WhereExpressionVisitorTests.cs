using System.Linq.Expressions;
using FireBlazor.Platform.Wasm;

namespace FireBlazor.Tests.Firestore;

public class WhereExpressionVisitorTests
{
    [Fact]
    public void Visit_EqualOperator_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "John";
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("name", visitor.Clauses[0].Field);
        Assert.Equal("==", visitor.Clauses[0].Operator);
        Assert.Equal("John", visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_NotEqualOperator_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name != "John";
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("!=", visitor.Clauses[0].Operator);
    }

    [Fact]
    public void Visit_GreaterThan_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age > 18;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("age", visitor.Clauses[0].Field);
        Assert.Equal(">", visitor.Clauses[0].Operator);
        Assert.Equal(18, visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_GreaterThanOrEqual_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= 21;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal(">=", visitor.Clauses[0].Operator);
    }

    [Fact]
    public void Visit_LessThan_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age < 65;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("<", visitor.Clauses[0].Operator);
    }

    [Fact]
    public void Visit_LessThanOrEqual_ExtractsWhereClause()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Age <= 65;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("<=", visitor.Clauses[0].Operator);
    }

    [Fact]
    public void Visit_AndAlso_ExtractsMultipleClauses()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Name == "John" && x.Age > 18;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Equal(2, visitor.Clauses.Count);
    }

    [Fact]
    public void Visit_ReversedComparison_HandlesCorrectly()
    {
        Expression<Func<TestDocument, bool>> expr = x => 18 < x.Age;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("age", visitor.Clauses[0].Field);
        Assert.Equal(">", visitor.Clauses[0].Operator);
        Assert.Equal(18, visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_ContainsOnArrayProperty_ExtractsArrayContains()
    {
        Expression<Func<TestDocument, bool>> expr = x => x.Tags.Contains("featured");
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("tags", visitor.Clauses[0].Field);
        Assert.Equal("array-contains", visitor.Clauses[0].Operator);
        Assert.Equal("featured", visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_ArrayContainsWithVariable_ExtractsValue()
    {
        var tagToFind = "popular";
        Expression<Func<TestDocument, bool>> expr = x => x.Tags.Contains(tagToFind);
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("array-contains", visitor.Clauses[0].Operator);
        Assert.Equal("popular", visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_InOperator_ExtractsInClause()
    {
        var validStatuses = new[] { "active", "pending" };
        Expression<Func<TestDocument, bool>> expr = x => validStatuses.Contains(x.Status);
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("status", visitor.Clauses[0].Field);
        Assert.Equal("in", visitor.Clauses[0].Operator);
        Assert.Equal(validStatuses, visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_NotInOperator_ExtractsNotInClause()
    {
        var excludedStatuses = new[] { "deleted", "archived" };
        Expression<Func<TestDocument, bool>> expr = x => !excludedStatuses.Contains(x.Status);
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal("status", visitor.Clauses[0].Field);
        Assert.Equal("not-in", visitor.Clauses[0].Operator);
        Assert.Equal(excludedStatuses, visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_CapturedVariable_ExtractsValue()
    {
        var minAge = 21;
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= minAge;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal(21, visitor.Clauses[0].Value);
    }

    [Fact]
    public void Visit_PropertyOfCapturedObject_ExtractsValue()
    {
        var criteria = new { MinAge = 18 };
        Expression<Func<TestDocument, bool>> expr = x => x.Age >= criteria.MinAge;
        var visitor = new WhereExpressionVisitor();

        visitor.Visit(expr);

        Assert.Single(visitor.Clauses);
        Assert.Equal(18, visitor.Clauses[0].Value);
    }
}
