using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MiCake.Util.Query.Dynamic;
using Xunit;

namespace MiCake.Core.Tests.Util.LinqFilter;

public class ExpressionHelpers_Tests
{
    [Fact]
    public void BuildNestedPropertyExpression_ShouldBuildCorrectExpression()
    {
        var param = Expression.Parameter(typeof(Dummy), "x");
        var expr = ExpressionHelpers.BuildNestedPropertyExpression(param, "Child.Value");
        Assert.NotNull(expr);
        Assert.Equal("x.Child.Value", expr.ToString());
    }

    [Fact]
    public void BuildNestedPropertyExpression_ShouldThrowOnNullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => ExpressionHelpers.BuildNestedPropertyExpression(null, "Child.Value"));
    }

    [Fact]
    public void BuildNestedPropertyExpression_ShouldThrowOnEmptyPropertyName()
    {
        var param = Expression.Parameter(typeof(Dummy), "x");
        Assert.Throws<ArgumentException>(() => ExpressionHelpers.BuildNestedPropertyExpression(param, ""));
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnEqualExpression()
    {
        var left = Expression.Constant(5);
        var right = Expression.Constant(5);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.Equal);
        Assert.Equal(ExpressionType.Equal, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnNotEqualExpression()
    {
        var left = Expression.Constant(5);
        var right = Expression.Constant(10);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.NotEqual);
        Assert.Equal(ExpressionType.NotEqual, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnGreaterThanExpression()
    {
        var left = Expression.Constant(10);
        var right = Expression.Constant(5);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.GreaterThan);
        Assert.Equal(ExpressionType.GreaterThan, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnLessThanExpression()
    {
        var left = Expression.Constant(5);
        var right = Expression.Constant(10);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.LessThan);
        Assert.Equal(ExpressionType.LessThan, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnAndAlsoExpression()
    {
        var left = Expression.Constant(true);
        var right = Expression.Constant(false);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, FilterJoinType.And);
        Assert.Equal(ExpressionType.AndAlso, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldReturnOrElseExpression()
    {
        var left = Expression.Constant(true);
        var right = Expression.Constant(false);
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, FilterJoinType.Or);
        Assert.Equal(ExpressionType.OrElse, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldThrowOnNullLeft()
    {
        var right = Expression.Constant(5);
        Assert.Throws<ArgumentNullException>(() => ExpressionHelpers.ConcatExpressionsWithOperator(null, right, ValueOperatorType.Equal));
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldThrowOnNullRight()
    {
        var left = Expression.Constant(5);
        Assert.Throws<ArgumentNullException>(() => ExpressionHelpers.ConcatExpressionsWithOperator(left, null, ValueOperatorType.Equal));
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldHandleInOperatorForList()
    {
        var left = Expression.Constant("A");
        var right = Expression.Constant(new List<string> { "A", "B", "C" });
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.In);
        Assert.Equal(ExpressionType.Call, expr.NodeType);
    }

    [Fact]
    public void ConcatExpressionsWithOperator_ShouldHandleContainsOperatorForString()
    {
        var left = Expression.Constant("MiCake");
        var right = Expression.Constant("Cake");
        var expr = ExpressionHelpers.ConcatExpressionsWithOperator(left, right, ValueOperatorType.Contains);
        Assert.Equal(ExpressionType.Call, expr.NodeType);
    }

    private class Dummy
    {
        public DummyChild Child { get; set; } = new DummyChild();
    }

    private class DummyChild
    {
        public int Value { get; set; } = 42;
    }
}