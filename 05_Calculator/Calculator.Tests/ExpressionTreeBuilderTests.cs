using Calculator.Core.Models;
using Calculator.Core.Services;

namespace Calculator.Tests;

[TestClass]
public class ExpressionTreeBuilderTests
{
    private readonly ExpressionTreeBuilder _builder = new();

    [TestMethod]
    public void BuildGraph_SimpleArithmetic_BuildsAstTree()
    {
        // 3 + 5 * 2 = 13
        var graph = _builder.BuildGraph("3 + 5 * 2", fullParseTree: false);

        Assert.IsNotNull(graph);
        Assert.AreEqual("13", graph.Result);
        StringAssert.Contains(graph.Rpn, "3 5 2 * +");
        Assert.IsTrue(graph.Nodes.Count > 0);
        Assert.IsTrue(graph.Edges.Count > 0);

        // Root in AST should be the last evaluated operator '+'
        var plusNode = graph.Nodes.FirstOrDefault(n => n.Label == "+" && n.Type == ExpressionTreeNodeType.Operator);
        Assert.IsNotNull(plusNode);

        // Canvas dimensions should be positive
        Assert.IsTrue(graph.CanvasWidth > 0);
        Assert.IsTrue(graph.CanvasHeight > 0);
    }

    [TestMethod]
    public void BuildGraph_FullParseTree_BuildsHierarchyETF()
    {
        // (5 + 3) * 2 = 16
        var graph = _builder.BuildGraph("(5 + 3) * 2", fullParseTree: true);

        Assert.IsNotNull(graph);
        Assert.AreEqual("16", graph.Result);

        // In Session 5 Grammar V2: E -> T -> F
        bool hasExpressionNode = graph.Nodes.Any(n => n.Label == "E" || n.Type == ExpressionTreeNodeType.Expression);
        bool hasTermNode = graph.Nodes.Any(n => n.Label == "T" || n.Type == ExpressionTreeNodeType.Term);
        bool hasFactorNode = graph.Nodes.Any(n => n.Label == "F" || n.Type == ExpressionTreeNodeType.Factor);

        Assert.IsTrue(hasExpressionNode, "Parse Tree must have Expression (E) nodes");
        Assert.IsTrue(hasTermNode, "Parse Tree must have Term (T) nodes");
        Assert.IsTrue(hasFactorNode, "Parse Tree must have Factor (F) nodes");

        // Edges count should be at least nodes count - 1 (tree topology)
        Assert.IsTrue(graph.Edges.Count >= graph.Nodes.Count - 1);
    }

    [TestMethod]
    public void BuildGraph_PrecedenceAndParentheses_AstStructureIsCorrect()
    {
        // (10 - 4) * 3 = 18
        var graph = _builder.BuildGraph("(10 - 4) * 3", fullParseTree: false);

        Assert.IsNotNull(graph);
        Assert.AreEqual("18", graph.Result);
        Assert.AreEqual("10 4 - 3 *", graph.Rpn);

        // Multiplier should be present
        var multNode = graph.Nodes.FirstOrDefault(n => n.Label == "*");
        Assert.IsNotNull(multNode);
    }

    [TestMethod]
    public void BuildGraph_SingleNumberAndEmpty_HandledGracefully()
    {
        var graphEmpty = _builder.BuildGraph("", fullParseTree: false);
        Assert.IsNotNull(graphEmpty);
        Assert.AreEqual("0", graphEmpty.Result);

        var graphSingle = _builder.BuildGraph("42", fullParseTree: false);
        Assert.IsNotNull(graphSingle);
        Assert.AreEqual("42", graphSingle.Result);
        Assert.AreEqual(1, graphSingle.Nodes.Count);
        Assert.AreEqual(0, graphSingle.Edges.Count);
    }

    [TestMethod]
    public void BuildGraph_EdgeCoordinates_AreWithinCanvasBounds()
    {
        var graph = _builder.BuildGraph("7 + 8 * 9", fullParseTree: false);

        foreach (var edge in graph.Edges)
        {
            Assert.IsTrue(edge.X1 >= 0);
            Assert.IsTrue(edge.Y1 >= 0);
            Assert.IsTrue(edge.X2 >= 0);
            Assert.IsTrue(edge.Y2 >= 0);
        }

        foreach (var node in graph.Nodes)
        {
            Assert.IsTrue(node.CanvasLeft >= 0);
            Assert.IsTrue(node.CanvasTop >= 0);
            Assert.IsNotNull(node.BackgroundColor);
            Assert.IsNotNull(node.BorderColor);
        }
    }
}
