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
    public string DisplaySnapshot { get; set; } = string.Empty;
    public string ExpressionSnapshot { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class AstNode
{
    public string Value { get; set; } = string.Empty;
    public string NodeType { get; set; } = "Operand"; // "Operand", "BinaryOperator", "Function", "UnaryOperator"
    public AstNode? Left { get; set; }
    public AstNode? Right { get; set; }
    public int Depth { get; set; }

    public override string ToString() => Value;
}

public class ShuntingYardTraceStep
{
    public int Step { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string OperatorStack { get; set; } = string.Empty;
    public string OutputQueue { get; set; } = string.Empty;
}

public class PropertyCheckResult
{
    public string PropertyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Safety"; // Safety or Liveness
    public string FormalFormula { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public string Evidence { get; set; } = string.Empty;
}

public class ModelVerificationReport
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool AllPropertiesSatisfied { get; set; }
    public int TotalProperties { get; set; }
    public int PassedProperties { get; set; }
    public int TransitionsAudited { get; set; }
    public List<PropertyCheckResult> Results { get; set; } = new();
}

public class ProgrammerValueRepresentation
{
    public ulong RawValue { get; set; }
    public string Hex { get; set; } = "0";
    public string Dec { get; set; } = "0";
    public string Oct { get; set; } = "0";
    public string Bin { get; set; } = "0";
    public string FormattedBin { get; set; } = "0000 0000";
}

public enum ExpressionTreeNodeType
{
    Expression, // E
    Term,       // T
    Factor,     // F
    Operator,   // +, -, *, /, ^
    Number,     // 1, 2.5
    Function,   // sin, cos, sqrt
    Parenthesis // (, )
}

public class ExpressionTreeNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ExpressionTreeNodeType Type { get; set; } = ExpressionTreeNodeType.Number;
    public double? Value { get; set; }
    public List<ExpressionTreeNode> Children { get; set; } = new();

    // Coordinates for Canvas rendering
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 46;
    public double Height { get; set; } = 34;

    public double CanvasLeft => Math.Max(0, X - (Width / 2.0));
    public double CanvasTop => Math.Max(0, Y - (Height / 2.0));

    public string BackgroundColor => Type switch
    {
        ExpressionTreeNodeType.Expression => "#0B3954",  // Deep Blue
        ExpressionTreeNodeType.Term => "#087E8B",        // Teal
        ExpressionTreeNodeType.Factor => "#3C1B58",      // Purple
        ExpressionTreeNodeType.Operator => "#1f5ca9",    // Signature Sapphire Blue
        ExpressionTreeNodeType.Number => "#1A2234",      // Dark Slate Card
        ExpressionTreeNodeType.Function => "#065F46",    // Emerald
        _ => "#1E293B"
    };

    public string BorderColor => Type switch
    {
        ExpressionTreeNodeType.Expression => "#38BDF8",
        ExpressionTreeNodeType.Term => "#2DD4BF",
        ExpressionTreeNodeType.Factor => "#C084FC",
        ExpressionTreeNodeType.Operator => "#4ea2ff",
        ExpressionTreeNodeType.Number => "#334155",
        ExpressionTreeNodeType.Function => "#34D399",
        _ => "#475569"
    };
}

public class ExpressionTreeEdge
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}

public class ExpressionTreeGraph
{
    public string Expression { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Rpn { get; set; } = string.Empty;
    public List<ExpressionTreeNode> Nodes { get; set; } = new();
    public List<ExpressionTreeEdge> Edges { get; set; } = new();
    public double CanvasWidth { get; set; } = 600;
    public double CanvasHeight { get; set; } = 400;
}

// ─────────────────────────────────────────────────────────────────────────────
// Automata Visualizer Models (DFA & NFA - CT287 Model Checking)
// ─────────────────────────────────────────────────────────────────────────────

public class AutomataNode
{
    public string Id { get; set; } = string.Empty;
    public string StateKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsStart { get; set; }
    public bool IsAccepting { get; set; }
    public bool IsError { get; set; }
    public bool IsActive { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Diameter { get; set; } = 82;

    public double CanvasLeft => Math.Max(0, X - (Diameter / 2.0));
    public double CanvasTop => Math.Max(0, Y - (Diameter / 2.0));
    public double InnerDiameter => Math.Max(0, Diameter - 12);
    public double InnerCanvasLeft => Math.Max(0, X - (InnerDiameter / 2.0));
    public double InnerCanvasTop => Math.Max(0, Y - (InnerDiameter / 2.0));

    // Outer circle colors
    public string BackgroundColor => IsActive ? "#1f5ca9" : (IsError ? "#3b1116" : (IsAccepting ? "#0b2b1d" : "#131b2a"));
    public string BorderColor => IsActive ? "#60a5fa" : (IsError ? "#ef4444" : (IsAccepting ? "#10b981" : "#334155"));
    public double BorderThickness => IsActive ? 3.0 : 2.0;

    // Glowing shadow for active node
    public string ShadowColor => IsActive ? "#60a5fa" : "Transparent";
}

public class AutomataEdge
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSelfLoop { get; set; }
    public bool IsActive { get; set; }

    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    // Label positioning
    public double LabelX { get; set; }
    public double LabelY { get; set; }

    // Arrowhead coordinates (triangle)
    public double ArrowTipX { get; set; }
    public double ArrowTipY { get; set; }
    public double ArrowLeftX { get; set; }
    public double ArrowLeftY { get; set; }
    public double ArrowRightX { get; set; }
    public double ArrowRightY { get; set; }

    public string ArrowPoints => $"{ArrowTipX},{ArrowTipY} {ArrowLeftX},{ArrowLeftY} {ArrowRightX},{ArrowRightY}";
    public string StrokeColor => IsActive ? "#38bdf8" : "#475569";
    public double StrokeThickness => IsActive ? 2.5 : 1.5;
}

public class AutomataGraph
{
    public string Title { get; set; } = string.Empty;
    public string ModeBadge { get; set; } = "DFA";
    public string FormalDefinition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActiveStateName { get; set; } = string.Empty;
    public List<AutomataNode> Nodes { get; set; } = new();
    public List<AutomataEdge> Edges { get; set; } = new();
    public double CanvasWidth { get; set; } = 860;
    public double CanvasHeight { get; set; } = 480;
}
