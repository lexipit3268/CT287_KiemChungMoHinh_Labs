using Calculator.Core.Enums;
using Calculator.Core.Parser;

namespace Calculator.Tests;

[TestClass]
public class ExpressionEvaluatorTests
{
    private ExpressionEvaluator _evaluator = null!;

    [TestInitialize]
    public void Setup()
    {
        _evaluator = new ExpressionEvaluator();
    }

    [TestMethod]
    public void TestBasicArithmeticAndPrecedence()
    {
        var res1 = _evaluator.Evaluate("2 + 3 * 4");
        Assert.IsTrue(res1.IsSuccess);
        Assert.AreEqual("14", res1.DisplayValue);

        var res2 = _evaluator.Evaluate("(2 + 3) * 4");
        Assert.IsTrue(res2.IsSuccess);
        Assert.AreEqual("20", res2.DisplayValue);
    }

    [TestMethod]
    public void TestLeftAssociativity_SubtractionAndDivision()
    {
        // 10 - 4 - 2 should be (10 - 4) - 2 = 4, NOT 10 - (4 - 2) = 8
        var res1 = _evaluator.Evaluate("10 - 4 - 2");
        Assert.IsTrue(res1.IsSuccess);
        Assert.AreEqual("4", res1.DisplayValue);

        // 24 / 4 / 2 should be (24 / 4) / 2 = 3
        var res2 = _evaluator.Evaluate("24 / 4 / 2");
        Assert.IsTrue(res2.IsSuccess);
        Assert.AreEqual("3", res2.DisplayValue);
    }

    [TestMethod]
    public void TestExponentiation_RightAssociativity()
    {
        // 2 ^ 3 ^ 2 should be 2 ^ (3 ^ 2) = 2 ^ 9 = 512
        var res = _evaluator.Evaluate("2 ^ 3 ^ 2");
        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual("512", res.DisplayValue);
    }

    [TestMethod]
    public void TestImplicitMultiplication()
    {
        var res1 = _evaluator.Evaluate("2(3 + 4)");
        Assert.IsTrue(res1.IsSuccess);
        Assert.AreEqual("14", res1.DisplayValue);

        var res2 = _evaluator.Evaluate("(2 + 3)(4 + 1)");
        Assert.IsTrue(res2.IsSuccess);
        Assert.AreEqual("25", res2.DisplayValue);
    }

    [TestMethod]
    public void TestTrigonometry_DegreeMode()
    {
        _evaluator.AngleMode = AngleUnit.Degree;

        var sin30 = _evaluator.Evaluate("sin(30)");
        Assert.IsTrue(sin30.IsSuccess);
        Assert.AreEqual(0.5, sin30.Value, 1e-9);

        var cos60 = _evaluator.Evaluate("cos(60)");
        Assert.IsTrue(cos60.IsSuccess);
        Assert.AreEqual(0.5, cos60.Value, 1e-9);

        var tan45 = _evaluator.Evaluate("tan(45)");
        Assert.IsTrue(tan45.IsSuccess);
        Assert.AreEqual(1.0, tan45.Value, 1e-9);
    }

    [TestMethod]
    public void TestTrigonometry_RadianMode()
    {
        _evaluator.AngleMode = AngleUnit.Radian;

        var sinPiOver2 = _evaluator.Evaluate("sin(pi / 2)");
        Assert.IsTrue(sinPiOver2.IsSuccess);
        Assert.AreEqual(1.0, sinPiOver2.Value, 1e-9);
    }

    [TestMethod]
    public void TestFactorial()
    {
        var res0 = _evaluator.Evaluate("0!");
        Assert.IsTrue(res0.IsSuccess);
        Assert.AreEqual("1", res0.DisplayValue);

        var res5 = _evaluator.Evaluate("5!");
        Assert.IsTrue(res5.IsSuccess);
        Assert.AreEqual("120", res5.DisplayValue);

        var resComb = _evaluator.Evaluate("3! + 4!");
        Assert.IsTrue(resComb.IsSuccess);
        Assert.AreEqual("30", resComb.DisplayValue);
    }

    [TestMethod]
    public void TestLogAndSquareRoot()
    {
        var resLog = _evaluator.Evaluate("log(100)");
        Assert.IsTrue(resLog.IsSuccess);
        Assert.AreEqual("2", resLog.DisplayValue);

        var resLn = _evaluator.Evaluate("ln(e)");
        Assert.IsTrue(resLn.IsSuccess);
        Assert.AreEqual("1", resLn.DisplayValue);

        var resSqrt = _evaluator.Evaluate("sqrt(81)");
        Assert.IsTrue(resSqrt.IsSuccess);
        Assert.AreEqual("9", resSqrt.DisplayValue);
    }

    [TestMethod]
    public void TestDivisionByZero_ReturnsError()
    {
        var res = _evaluator.Evaluate("50 / 0");
        Assert.IsFalse(res.IsSuccess);
        Assert.IsTrue(res.ErrorMessage?.Contains("divide by zero", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TestNegativeSquareRoot_ReturnsError()
    {
        var res = _evaluator.Evaluate("sqrt(-16)");
        Assert.IsFalse(res.IsSuccess);
        Assert.IsTrue(res.ErrorMessage?.Contains("negative", StringComparison.OrdinalIgnoreCase));
    }
}
