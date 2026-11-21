using Calculator.Core.Enums;
using Calculator.Core.StateMachine;

namespace Calculator.Tests;

[TestClass]
public class StateMachineTests
{
    [TestMethod]
    public void TestInitialState_IsStart_WithZeroDisplay()
    {
        var fsm = new CalculatorStateMachine();
        Assert.AreEqual(CalculatorState.Start, fsm.CurrentState);
        Assert.AreEqual("0", fsm.Display);
        Assert.AreEqual(string.Empty, fsm.ExpressionBuffer);
        Assert.AreEqual(0, fsm.OpenParenCount);
    }

    [TestMethod]
    public void TestDigitInput_TransitionsToInputOperand()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('5');
        Assert.AreEqual(CalculatorState.InputOperand, fsm.CurrentState);
        Assert.AreEqual("5", fsm.Display);

        fsm.ProcessDigit('2');
        Assert.AreEqual("52", fsm.Display);
    }

    [TestMethod]
    public void TestDecimalPoint_AllowsOnlyOneDotPerOperand()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('3');
        bool firstDot = fsm.ProcessDecimalPoint();
        Assert.IsTrue(firstDot);
        Assert.AreEqual("3.", fsm.Display);

        fsm.ProcessDigit('1');
        fsm.ProcessDigit('4');
        Assert.AreEqual("3.14", fsm.Display);

        // Automaton must block second dot!
        bool secondDot = fsm.ProcessDecimalPoint();
        Assert.IsFalse(secondDot);
        Assert.AreEqual("3.14", fsm.Display);
    }

    [TestMethod]
    public void TestOperatorReplacement_WhenConsecutiveOperatorsPressed()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('5');
        fsm.ProcessBinaryOperator("+");
        Assert.AreEqual(CalculatorState.OperatorEntered, fsm.CurrentState);
        Assert.IsTrue(fsm.ExpressionBuffer.Contains("+"));

        // Press * immediately -> replaces +
        fsm.ProcessBinaryOperator("*");
        Assert.AreEqual(CalculatorState.OperatorEntered, fsm.CurrentState);
        Assert.IsTrue(fsm.ExpressionBuffer.Contains("*"));
        Assert.IsFalse(fsm.ExpressionBuffer.Contains("+"));

        fsm.ProcessDigit('3');
        var result = fsm.Evaluate();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("15", result.DisplayValue);
    }

    [TestMethod]
    public void TestParentheses_OpenAndClose_BalanceChecked()
    {
        var fsm = new CalculatorStateMachine();
        // Cannot close paren when count is 0
        Assert.IsFalse(fsm.CanEnterCloseParen);
        bool closedInvalid = fsm.ProcessCloseParen();
        Assert.IsFalse(closedInvalid);

        fsm.ProcessOpenParen();
        Assert.AreEqual(1, fsm.OpenParenCount);
        Assert.AreEqual(CalculatorState.OpenParenEntered, fsm.CurrentState);

        fsm.ProcessDigit('4');
        fsm.ProcessBinaryOperator("+");
        fsm.ProcessDigit('6');
        Assert.IsTrue(fsm.CanEnterCloseParen);

        bool closedValid = fsm.ProcessCloseParen();
        Assert.IsTrue(closedValid);
        Assert.AreEqual(0, fsm.OpenParenCount);
        Assert.AreEqual(CalculatorState.CloseParenEntered, fsm.CurrentState);

        fsm.ProcessBinaryOperator("*");
        fsm.ProcessDigit('2');
        var res = fsm.Evaluate();
        Assert.AreEqual("20", res.DisplayValue);
    }

    [TestMethod]
    public void TestAutoCloseParentheses_OnEvaluation()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessOpenParen(); // (
        fsm.ProcessDigit('1');
        fsm.ProcessDigit('0');  // 10
        fsm.ProcessBinaryOperator("-");
        fsm.ProcessDigit('3');  // 3
        // Unclosed paren left! (10 - 3
        Assert.AreEqual(1, fsm.OpenParenCount);

        var result = fsm.Evaluate();
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("7", result.DisplayValue);
        Assert.AreEqual(0, fsm.OpenParenCount);
    }

    [TestMethod]
    public void TestBackspace_AndClear()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('9');
        fsm.ProcessDigit('8');
        fsm.ProcessDigit('7');
        Assert.AreEqual("987", fsm.Display);

        fsm.Backspace();
        Assert.AreEqual("98", fsm.Display);

        fsm.ClearEntry();
        Assert.AreEqual("0", fsm.Display);

        fsm.Reset();
        Assert.AreEqual(CalculatorState.Start, fsm.CurrentState);
        Assert.AreEqual("0", fsm.Display);
    }

    [TestMethod]
    public void TestTransitionLog_AuditTrailRecorded()
    {
        var fsm = new CalculatorStateMachine();
        fsm.ProcessDigit('7');
        fsm.ProcessBinaryOperator("+");
        fsm.ProcessDigit('3');
        fsm.Evaluate();

        Assert.IsTrue(fsm.TransitionLog.Count >= 4);
        var lastRecord = fsm.TransitionLog[^1];
        Assert.AreEqual(CalculatorState.Evaluated, lastRecord.ToState);
        Assert.AreEqual(CalculatorInputType.Equals, lastRecord.InputType);
    }

    [TestMethod]
    public void TestLoadCalculation_RecallsExpressionAndResult_AndAllowsContinuedComputation()
    {
        var fsm = new CalculatorStateMachine();
        // Recall "25 * 4" with result "100"
        fsm.LoadCalculation("25 * 4", "100");

        Assert.AreEqual("100", fsm.Display);
        Assert.AreEqual("25 * 4 =", fsm.ExpressionBuffer);
        Assert.AreEqual(CalculatorState.Evaluated, fsm.CurrentState);

        // Continue calculation: + 50 =
        fsm.ProcessBinaryOperator("+");
        Assert.AreEqual(CalculatorState.OperatorEntered, fsm.CurrentState);
        fsm.ProcessDigit('5');
        fsm.ProcessDigit('0');
        var res = fsm.Evaluate();

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual("150", res.DisplayValue);
    }

    [TestMethod]
    public void TestLargeNumberInput_Allows20PlusDigits()
    {
        var fsm = new CalculatorStateMachine();
        // Type 25 consecutive '3's
        for (int i = 0; i < 25; i++)
        {
            fsm.ProcessDigit('3');
        }

        Assert.AreEqual(25, fsm.Display.Length);
        Assert.AreEqual(new string('3', 25), fsm.Display);
        Assert.AreEqual(CalculatorState.InputOperand, fsm.CurrentState);

        // Can continue typing up to 40 digits
        for (int i = 0; i < 15; i++)
        {
            fsm.ProcessDigit('7');
        }

        Assert.AreEqual(40, fsm.Display.Length);
        Assert.AreEqual(CalculatorState.InputOperand, fsm.CurrentState);
    }
}
