using Calculator.Core.Enums;
using Calculator.Core.Services;

namespace Calculator.Tests;

[TestClass]
public class AutomataTests
{
    [TestMethod]
    public void TestDfaForExpression_GeneratesSpecificStatesForCalculation()
    {
        var service = new AutomataBuilderService();
        // Calculation: 8 * 9
        var graph = service.BuildDfaForExpression("8 * 9", CalculatorState.Evaluated, "72");

        Assert.IsNotNull(graph);
        Assert.IsTrue(graph.Title.Contains("8 * 9"));
        // Tokens: 8, *, 9 -> Nodes: S0 (Start), S1 [8], S2 [*], S3 [9], S4 (Accept [72]), S_TRAP (Error)
        Assert.AreEqual(6, graph.Nodes.Count);

        var startNode = graph.Nodes.First(n => n.Id == "S0");
        Assert.IsTrue(startNode.IsStart);

        var acceptNode = graph.Nodes.First(n => n.IsAccepting);
        Assert.IsTrue(acceptNode.Title.Contains("72"));

        var errorNode = graph.Nodes.First(n => n.IsError);
        Assert.AreEqual("S_TRAP", errorNode.Id);

        // Verify active state highlight when evaluated
        Assert.IsTrue(acceptNode.IsActive);
    }

    [TestMethod]
    public void TestNfaForExpression_GeneratesThompsonEpsilonBranches()
    {
        var service = new AutomataBuilderService();
        // Calculation: 5 + 6
        var graph = service.BuildNfaForExpression("5 + 6");

        Assert.IsNotNull(graph);
        Assert.IsTrue(graph.Title.Contains("5 + 6"));

        // Has Start state
        var start = graph.Nodes.First(n => n.IsStart);
        Assert.AreEqual("q0", start.Id);

        // Has Accepting state at end of calculation
        var accepting = graph.Nodes.Where(n => n.IsAccepting).ToList();
        Assert.AreEqual(1, accepting.Count);

        // Edges contain epsilon (ε) transitions
        Assert.IsTrue(graph.Edges.Any(e => e.Label == "ε"));
        Assert.IsTrue(graph.Edges.Any(e => e.Label == "5"));
        Assert.IsTrue(graph.Edges.Any(e => e.Label == "+"));
        Assert.IsTrue(graph.Edges.Any(e => e.Label == "6"));
    }

    [TestMethod]
    public void TestDfaForSubtraction_DoesNotLabelMinusAsInvalidInput()
    {
        var service = new AutomataBuilderService();
        // Calculation: 8 - 9 = -1
        var graph = service.BuildDfaForExpression("8 - 9", CalculatorState.Evaluated, "-1");

        Assert.IsNotNull(graph);
        // Ensure '-' is a valid operator state
        var minusNode = graph.Nodes.FirstOrDefault(n => n.Title.Contains("[-]"));
        Assert.IsNotNull(minusNode);
        Assert.AreEqual("Toán tử", minusNode.Subtitle);

        // Ensure there is NO edge with label "invalid input"
        Assert.IsFalse(graph.Edges.Any(e => e.Label.Equals("invalid input", System.StringComparison.OrdinalIgnoreCase)),
            "The edge label must not be 'invalid input' so as not to confuse the user into thinking the operator is invalid.");

        // Edge between S0 and S1 is '8', S1 and S2 is '-', S2 and S3 is '9'
        Assert.IsTrue(graph.Edges.Any(e => e.Label == "-"));
    }
}
