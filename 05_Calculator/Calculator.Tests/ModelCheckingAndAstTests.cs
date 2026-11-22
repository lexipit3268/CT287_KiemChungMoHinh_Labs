using Calculator.Core.Enums;
using Calculator.Core.Services;
using Calculator.Core.StateMachine;

namespace Calculator.Tests;

[TestClass]
public class ModelCheckingAndAstTests
{
    [TestMethod]
    public void TestModelChecker_All7PropertiesSatisfiedOnFreshFsm()
    {
        var fsm = new CalculatorStateMachine();
        var checker = new ModelCheckingService();

        var report = checker.Verify(fsm);

        Assert.IsTrue(report.AllPropertiesSatisfied, "All formal properties should be satisfied.");
        Assert.AreEqual(7, report.TotalProperties);
        Assert.AreEqual(7, report.PassedProperties);

        // Verify specific properties
        var propParen = report.Results.First(r => r.PropertyId == "PROP-01");
        Assert.IsTrue(propParen.IsSatisfied);
        Assert.AreEqual("Safety", propParen.Category);

        var propZeroDiv = report.Results.First(r => r.PropertyId == "PROP-03");
        Assert.IsTrue(propZeroDiv.IsSatisfied);

        var propDeadlock = report.Results.First(r => r.PropertyId == "PROP-05");
        Assert.IsTrue(propDeadlock.IsSatisfied);
        Assert.AreEqual("Liveness", propDeadlock.Category);
    }

    [TestMethod]
    public void TestModelChecker_AfterArithmeticCalculations_PreservesInvariants()
    {
        var fsm = new CalculatorStateMachine();
        var checker = new ModelCheckingService();

        // Perform complex calculation: (10 + 5) * 2 / 5 =
        fsm.ProcessOpenParen();
        fsm.ProcessDigit('1');
        fsm.ProcessDigit('0');
        fsm.ProcessBinaryOperator("+");
        fsm.ProcessDigit('5');
        fsm.ProcessCloseParen();
        fsm.ProcessBinaryOperator("*");
        fsm.ProcessDigit('2');
        fsm.ProcessBinaryOperator("/");
        fsm.ProcessDigit('5');
        var res = fsm.Evaluate();

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual("6", res.DisplayValue);

        var report = checker.Verify(fsm);
        Assert.IsTrue(report.AllPropertiesSatisfied);
        Assert.IsTrue(report.TransitionsAudited >= 10);
    }

    [TestMethod]
    public void TestAstVisualizer_BuildsAstAndFormatsTree()
    {
        var visualizer = new AstVisualizerService();
        string expr = "(5 + 3) * 2";

        var ast = visualizer.BuildAst(expr);
        Assert.IsNotNull(ast);
        Assert.AreEqual("*", ast.Value);
        Assert.AreEqual("BinaryOperator", ast.NodeType);
        Assert.IsNotNull(ast.Left);
        Assert.AreEqual("+", ast.Left.Value);
        Assert.IsNotNull(ast.Right);
        Assert.AreEqual("2", ast.Right.Value);

        string treeStr = visualizer.FormatAstTree(ast);
        Assert.IsTrue(treeStr.Contains("*"));
        Assert.IsTrue(treeStr.Contains("+"));
        Assert.IsTrue(treeStr.Contains("5"));
    }

    [TestMethod]
    public void TestAstVisualizer_ShuntingYardTraceSteps()
    {
        var visualizer = new AstVisualizerService();
        string expr = "3 + 4 * 2";

        var steps = visualizer.GenerateShuntingYardTrace(expr);
        Assert.IsTrue(steps.Count >= 5);
        Assert.AreEqual("START", steps[0].Token);
        Assert.AreEqual("COMPLETE", steps[^1].Token);
        Assert.IsTrue(steps[^1].Action.Contains("3 4 2 * +"));
    }

    [TestMethod]
    public void TestTraceExporter_GraphvizAndJsonOutputs()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('7');
        fsm.ProcessBinaryOperator("+");
        fsm.ProcessDigit('8');

        // Test Graphviz .DOT export
        string dot = TraceExporter.ExportGraphvizDot(fsm.CurrentState);
        Assert.IsTrue(dot.Contains("digraph CalculatorAutomaton"));
        Assert.IsTrue(dot.Contains("S1"));
        Assert.IsTrue(dot.Contains("#1f5ca9"), "Active state S1 should be highlighted with sapphire blue color.");

        // Test JSON export
        string json = TraceExporter.ExportJsonTrace(fsm.TransitionLog);
        Assert.IsTrue(json.Contains("StepIndex"));
        Assert.IsTrue(json.Contains("FromState"));
        Assert.IsTrue(json.Contains("ToState"));
    }
}
