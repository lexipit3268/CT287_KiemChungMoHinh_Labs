using Calculator.Core.Enums;
using Calculator.Core.Services;

namespace Calculator.Tests;

[TestClass]
public class CalculatorEngineTests
{
    [TestMethod]
    public void TestEngine_CompleteCalculationCycleWithHistory()
    {
        var engine = new CalculatorEngine();
        Assert.AreEqual(CalculatorMode.Standard, engine.Mode);

        // Enter: 12 + 8 =
        engine.StateMachine.ProcessDigit('1');
        engine.StateMachine.ProcessDigit('2');
        engine.StateMachine.ProcessBinaryOperator("+");
        engine.StateMachine.ProcessDigit('8');

        var res = engine.EvaluateAndRecord();
        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual("20", res.DisplayValue);

        // Verify History was populated
        Assert.AreEqual(1, engine.History.Items.Count);
        Assert.AreEqual("20", engine.History.Items[0].Result);
    }

    [TestMethod]
    public void TestEngine_ScientificTrigAndMemoryIntegration()
    {
        var engine = new CalculatorEngine
        {
            Mode = CalculatorMode.Scientific,
            AngleMode = AngleUnit.Degree
        };

        // sin(30) = 0.5
        engine.StateMachine.ProcessFunction("sin");
        engine.StateMachine.ProcessDigit('3');
        engine.StateMachine.ProcessDigit('0');
        var res = engine.EvaluateAndRecord();

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(0.5, res.Value, 1e-9);

        // Store into Memory
        engine.Memory.Store(res.Value);
        Assert.IsTrue(engine.Memory.HasMemory);
        Assert.AreEqual(0.5, engine.Memory.Recall(), 1e-9);

        // Perform M+ 2.5
        engine.Memory.Add(2.5);
        Assert.AreEqual(3.0, engine.Memory.Recall(), 1e-9);
    }

    [TestMethod]
    public void TestEngine_SwitchingAngleUnits_AffectsTrigCalculations()
    {
        var engine = new CalculatorEngine();
        
        // In Degree mode: sin(90) = 1
        engine.AngleMode = AngleUnit.Degree;
        engine.StateMachine.ProcessFunction("sin");
        engine.StateMachine.ProcessDigit('9');
        engine.StateMachine.ProcessDigit('0');
        var resDeg = engine.EvaluateAndRecord();
        Assert.AreEqual("1", resDeg.DisplayValue);

        // Reset and switch to Radian mode
        engine.StateMachine.Reset();
        engine.AngleMode = AngleUnit.Radian;
        // In Radian mode: sin(pi / 2) = 1
        engine.StateMachine.ProcessFunction("sin");
        engine.StateMachine.ProcessConstant("pi");
        engine.StateMachine.ProcessBinaryOperator("/");
        engine.StateMachine.ProcessDigit('2');
        var resRad = engine.EvaluateAndRecord();
        Assert.AreEqual("1", resRad.DisplayValue);
    }
}
