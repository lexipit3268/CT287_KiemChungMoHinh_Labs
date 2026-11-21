namespace Calculator.Core.Models;

public enum TokenType
{
    Number,
    BinaryOperator,
    UnaryOperator,
    Function,
    Constant,
    LeftParen,
    RightParen
}

public class Token
{
    public TokenType Type { get; }
    public string Text { get; }
    public double Value { get; }
    public int Precedence { get; }
    public bool IsRightAssociative { get; }
    public int ParameterCount { get; }

    public Token(TokenType type, string text, double value = 0, int precedence = 0, bool isRightAssociative = false, int parameterCount = 1)
    {
        Type = type;
        Text = text;
        Value = value;
        Precedence = precedence;
        IsRightAssociative = isRightAssociative;
        ParameterCount = parameterCount;
    }

    public override string ToString() => Text;
}

public class CalculationResult
{
    public bool IsSuccess { get; }
    public double Value { get; }
    public string DisplayValue { get; }
    public string? ErrorMessage { get; }

    private CalculationResult(bool isSuccess, double value, string displayValue, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        DisplayValue = displayValue;
        ErrorMessage = errorMessage;
    }

    public static CalculationResult Ok(double value)
    {
        // Format cleanly, avoid e.g. 1.0000000000000002
        string display = FormatNumber(value);
        return new CalculationResult(true, value, display, null);
    }

    public static CalculationResult Fail(string error)
    {
        return new CalculationResult(false, 0, error, error);
    }

    public static string FormatNumber(double val)
    {
        if (double.IsNaN(val)) return "Error: Result is undefined";
        if (double.IsPositiveInfinity(val)) return "Error: Overflow (Positive Infinity)";
        if (double.IsNegativeInfinity(val)) return "Error: Overflow (Negative Infinity)";

        // Handle small floating artifacts around 0 (e.g. sin(180 deg) = 1.22e-16)
        if (Math.Abs(val) < 1e-14 && val != 0) val = 0;

        // Clean representation
        if (Math.Abs(val) >= 1e15 || (Math.Abs(val) < 1e-6 && val != 0))
        {
            return val.ToString("G10", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Standard decimal formatting
        string formatted = val.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture);
        return formatted == "-0" ? "0" : formatted;
    }
}

public class HistoryItem
{
    public DateTime Timestamp { get; }
    public string Expression { get; }
    public string Result { get; }

    public HistoryItem(string expression, string result)
    {
        Timestamp = DateTime.Now;
        Expression = expression;
        Result = result;
    }

    public override string ToString() => $"{Expression} = {Result}";
}

public class StateTransitionRecord
{
    public int StepIndex { get; set; }
    public Enums.CalculatorState FromState { get; set; }
    public Enums.CalculatorInputType InputType { get; set; }
    public string InputValue { get; set; } = string.Empty;
    public Enums.CalculatorState ToState { get; set; }
    public string ActionDescription { get; set; } = string.Empty;
}
