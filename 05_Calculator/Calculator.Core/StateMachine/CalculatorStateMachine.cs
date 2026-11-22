using System.Text;
using Calculator.Core.Enums;
using Calculator.Core.Models;
using Calculator.Core.Parser;

namespace Calculator.Core.StateMachine;

public class CalculatorStateMachine
{
    private readonly ExpressionEvaluator _evaluator;
    private readonly List<StateTransitionRecord> _transitionLog = new();
    private int _stepCounter = 0;
    private string? _lastOperator = null;
    private string? _lastOperand = null;

    public CalculatorState CurrentState { get; private set; } = CalculatorState.Start;
    public string Display { get; private set; } = "0";
    public string ExpressionBuffer { get; private set; } = string.Empty;
    public int OpenParenCount { get; private set; } = 0;
    public bool HasDecimalPoint { get; private set; } = false;
    public AngleUnit AngleMode
    {
        get => _evaluator.AngleMode;
        set => _evaluator.AngleMode = value;
    }

    public IReadOnlyList<StateTransitionRecord> TransitionLog => _transitionLog.AsReadOnly();

    public CalculatorStateMachine(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator ?? new ExpressionEvaluator();
        Reset();
    }

    public void Reset()
    {
        RecordTransition(CurrentState, CalculatorInputType.Clear, "RESET", CalculatorState.Start, "Calculator reset to initial state");
        CurrentState = CalculatorState.Start;
        Display = "0";
        ExpressionBuffer = string.Empty;
        OpenParenCount = 0;
        HasDecimalPoint = false;
        _lastOperator = null;
        _lastOperand = null;
    }

    public void LoadCalculation(string expression, string result)
    {
        var fromState = CurrentState;
        Display = result;
        ExpressionBuffer = expression.EndsWith("=") ? expression : $"{expression} =";
        CurrentState = CalculatorState.Evaluated;
        OpenParenCount = 0;
        HasDecimalPoint = result.Contains('.');
        RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Recalled calculation: {expression} = {result}");
    }

    public bool CanEnterDecimalPoint => !HasDecimalPoint && CurrentState != CalculatorState.CloseParenEntered;
    public bool CanEnterCloseParen => OpenParenCount > 0 && (CurrentState == CalculatorState.InputOperand || CurrentState == CalculatorState.CloseParenEntered);

    public bool ProcessDigit(char digit)
    {
        if (!char.IsDigit(digit)) return false;

        var fromState = CurrentState;

        switch (CurrentState)
        {
            case CalculatorState.Start:
            case CalculatorState.Evaluated:
                Display = digit.ToString();
                ExpressionBuffer = string.Empty;
                CurrentState = CalculatorState.InputOperand;
                HasDecimalPoint = false;
                _lastOperator = null;
                _lastOperand = null;
                break;

            case CalculatorState.InputOperand:
                if (Display == "0")
                {
                    Display = digit.ToString();
                }
                else
                {
                    // Allow up to 45 digits for large number entry
                    if (Display.Length < 45)
                    {
                        Display += digit;
                    }
                }
                break;

            case CalculatorState.OperatorEntered:
            case CalculatorState.OpenParenEntered:
            case CalculatorState.FunctionEntered:
                Display = digit.ToString();
                CurrentState = CalculatorState.InputOperand;
                HasDecimalPoint = false;
                break;

            case CalculatorState.CloseParenEntered:
                // Implicit multiplication: ) 5 -> ) * 5
                ExpressionBuffer += " * ";
                Display = digit.ToString();
                CurrentState = CalculatorState.InputOperand;
                HasDecimalPoint = false;
                break;

            case CalculatorState.Error:
                Display = digit.ToString();
                ExpressionBuffer = string.Empty;
                CurrentState = CalculatorState.InputOperand;
                HasDecimalPoint = false;
                break;
        }

        RecordTransition(fromState, CalculatorInputType.Digit, digit.ToString(), CurrentState, $"Appended digit '{digit}'");
        return true;
    }

    public bool ProcessDecimalPoint()
    {
        var fromState = CurrentState;

        if (CurrentState == CalculatorState.InputOperand)
        {
            if (HasDecimalPoint)
            {
                // Automaton guards against multiple decimal points in one number
                return false;
            }
            Display += ".";
            HasDecimalPoint = true;
            RecordTransition(fromState, CalculatorInputType.DecimalPoint, ".", CurrentState, "Appended decimal point to existing operand");
            return true;
        }

        if (CurrentState == CalculatorState.Start || CurrentState == CalculatorState.Evaluated || CurrentState == CalculatorState.Error)
        {
            Display = "0.";
            ExpressionBuffer = string.Empty;
            CurrentState = CalculatorState.InputOperand;
            HasDecimalPoint = true;
            RecordTransition(fromState, CalculatorInputType.DecimalPoint, "0.", CurrentState, "Started new operand with '0.'");
            return true;
        }

        if (CurrentState == CalculatorState.OperatorEntered || CurrentState == CalculatorState.OpenParenEntered || CurrentState == CalculatorState.FunctionEntered)
        {
            Display = "0.";
            CurrentState = CalculatorState.InputOperand;
            HasDecimalPoint = true;
            RecordTransition(fromState, CalculatorInputType.DecimalPoint, "0.", CurrentState, "Started operand after operator/paren with '0.'");
            return true;
        }

        if (CurrentState == CalculatorState.CloseParenEntered)
        {
            ExpressionBuffer += " * ";
            Display = "0.";
            CurrentState = CalculatorState.InputOperand;
            HasDecimalPoint = true;
            RecordTransition(fromState, CalculatorInputType.DecimalPoint, "* 0.", CurrentState, "Implicit multiplication: ) * 0.");
            return true;
        }

        return false;
    }

    public bool ProcessBinaryOperator(string op)
    {
        // Clear previous repeated-equals memory on new operator input
        _lastOperator = null;
        _lastOperand = null;

        // Standardize visual symbols
        string normalizedOp = op switch
        {
            "×" => "*",
            "÷" => "/",
            "−" => "-",
            _ => op
        };

        var fromState = CurrentState;

        switch (CurrentState)
        {
            case CalculatorState.Start:
            case CalculatorState.Evaluated:
                ExpressionBuffer = $"{Display} {normalizedOp} ";
                CurrentState = CalculatorState.OperatorEntered;
                break;

            case CalculatorState.InputOperand:
                ExpressionBuffer += $"{Display} {normalizedOp} ";
                CurrentState = CalculatorState.OperatorEntered;
                HasDecimalPoint = false;
                break;

            case CalculatorState.CloseParenEntered:
                ExpressionBuffer += $" {normalizedOp} ";
                CurrentState = CalculatorState.OperatorEntered;
                break;

            case CalculatorState.OperatorEntered:
                // Replace previous operator with the newly selected one
                string trimmed = ExpressionBuffer.TrimEnd();
                int lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace >= 0)
                {
                    ExpressionBuffer = trimmed[..lastSpace] + $" {normalizedOp} ";
                }
                else
                {
                    ExpressionBuffer = $"{normalizedOp} ";
                }
                break;

            case CalculatorState.OpenParenEntered:
            case CalculatorState.FunctionEntered:
                if (normalizedOp == "-")
                {
                    // Unary minus for paren start: e.g. (-5
                    Display = "-";
                    CurrentState = CalculatorState.InputOperand;
                }
                else
                {
                    return false; // Cannot start paren with +, *, /
                }
                break;

            case CalculatorState.Error:
                return false;
        }

        RecordTransition(fromState, CalculatorInputType.BinaryOperator, normalizedOp, CurrentState, $"Operator '{normalizedOp}' placed");
        return true;
    }

    public bool ProcessOpenParen()
    {
        var fromState = CurrentState;

        switch (CurrentState)
        {
            case CalculatorState.Start:
            case CalculatorState.Evaluated:
                ExpressionBuffer = "(";
                OpenParenCount = 1;
                CurrentState = CalculatorState.OpenParenEntered;
                Display = "0";
                break;

            case CalculatorState.InputOperand:
                // Implicit multiplication: 5( -> 5 * (
                ExpressionBuffer += $"{Display} * (";
                OpenParenCount++;
                CurrentState = CalculatorState.OpenParenEntered;
                Display = "0";
                HasDecimalPoint = false;
                break;

            case CalculatorState.CloseParenEntered:
                // Implicit multiplication: )( -> ) * (
                ExpressionBuffer += " * (";
                OpenParenCount++;
                CurrentState = CalculatorState.OpenParenEntered;
                Display = "0";
                break;

            case CalculatorState.OperatorEntered:
                ExpressionBuffer += "(";
                OpenParenCount++;
                CurrentState = CalculatorState.OpenParenEntered;
                Display = "0";
                break;

            case CalculatorState.OpenParenEntered:
            case CalculatorState.FunctionEntered:
                ExpressionBuffer += "(";
                OpenParenCount++;
                CurrentState = CalculatorState.OpenParenEntered;
                Display = "0";
                break;

            default:
                return false;
        }

        RecordTransition(fromState, CalculatorInputType.OpenParen, "(", CurrentState, $"Opened parenthesis (Count: {OpenParenCount})");
        return true;
    }

    public bool ProcessCloseParen()
    {
        if (OpenParenCount <= 0) return false;

        var fromState = CurrentState;

        if (CurrentState == CalculatorState.InputOperand)
        {
            ExpressionBuffer += $"{Display})";
            OpenParenCount--;
            CurrentState = CalculatorState.CloseParenEntered;
            HasDecimalPoint = false;
        }
        else if (CurrentState == CalculatorState.CloseParenEntered)
        {
            ExpressionBuffer += ")";
            OpenParenCount--;
            CurrentState = CalculatorState.CloseParenEntered;
        }
        else
        {
            return false;
        }

        RecordTransition(fromState, CalculatorInputType.CloseParen, ")", CurrentState, $"Closed parenthesis (Remaining: {OpenParenCount})");
        return true;
    }

    public bool ProcessFunction(string funcName)
    {
        var fromState = CurrentState;

        // Scientific functions: sin, cos, tan, sqrt, log, ln, abs, etc.
        switch (CurrentState)
        {
            case CalculatorState.Start:
            case CalculatorState.Evaluated:
                ExpressionBuffer = $"{funcName}(";
                OpenParenCount++;
                CurrentState = CalculatorState.FunctionEntered;
                Display = "0";
                break;

            case CalculatorState.InputOperand:
                // Implicit multiplication: 3sin(45) -> 3 * sin(45)
                ExpressionBuffer += $"{Display} * {funcName}(";
                OpenParenCount++;
                CurrentState = CalculatorState.FunctionEntered;
                Display = "0";
                HasDecimalPoint = false;
                break;

            case CalculatorState.CloseParenEntered:
                ExpressionBuffer += $" * {funcName}(";
                OpenParenCount++;
                CurrentState = CalculatorState.FunctionEntered;
                Display = "0";
                break;

            case CalculatorState.OperatorEntered:
            case CalculatorState.OpenParenEntered:
            case CalculatorState.FunctionEntered:
                ExpressionBuffer += $"{funcName}(";
                OpenParenCount++;
                CurrentState = CalculatorState.FunctionEntered;
                Display = "0";
                break;

            default:
                return false;
        }

        RecordTransition(fromState, CalculatorInputType.UnaryFunction, funcName, CurrentState, $"Applied scientific function '{funcName}('");
        return true;
    }

    public bool ProcessConstant(string constantSymbol)
    {
        var fromState = CurrentState;
        string symbol = constantSymbol.ToLowerInvariant() == "pi" ? "π" : constantSymbol;

        switch (CurrentState)
        {
            case CalculatorState.Start:
            case CalculatorState.Evaluated:
                ExpressionBuffer = symbol;
                CurrentState = CalculatorState.CloseParenEntered;
                Display = symbol == "π" ? Math.PI.ToString(System.Globalization.CultureInfo.InvariantCulture) : Math.E.ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;

            case CalculatorState.InputOperand:
                ExpressionBuffer += $"{Display} * {symbol}";
                CurrentState = CalculatorState.CloseParenEntered;
                Display = symbol == "π" ? Math.PI.ToString(System.Globalization.CultureInfo.InvariantCulture) : Math.E.ToString(System.Globalization.CultureInfo.InvariantCulture);
                HasDecimalPoint = false;
                break;

            case CalculatorState.CloseParenEntered:
                ExpressionBuffer += $" * {symbol}";
                CurrentState = CalculatorState.CloseParenEntered;
                Display = symbol == "π" ? Math.PI.ToString(System.Globalization.CultureInfo.InvariantCulture) : Math.E.ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;

            case CalculatorState.OperatorEntered:
            case CalculatorState.OpenParenEntered:
            case CalculatorState.FunctionEntered:
                ExpressionBuffer += symbol;
                CurrentState = CalculatorState.CloseParenEntered;
                Display = symbol == "π" ? Math.PI.ToString(System.Globalization.CultureInfo.InvariantCulture) : Math.E.ToString(System.Globalization.CultureInfo.InvariantCulture);
                break;

            default:
                return false;
        }

        RecordTransition(fromState, CalculatorInputType.Constant, symbol, CurrentState, $"Inserted constant '{symbol}'");
        return true;
    }

    public bool ProcessImmediateFunction(string func)
    {
        // One-click operations directly on current display: x^2, x^3, 1/x, sqrt, !, %
        if (CurrentState == CalculatorState.Error) return false;

        var fromState = CurrentState;

        if (double.TryParse(Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double currentVal))
        {
            string expr = func switch
            {
                "sqr" => $"({currentVal})^2",
                "cube" => $"({currentVal})^3",
                "recip" => $"1/({currentVal})",
                "sqrt" => $"sqrt({currentVal})",
                "cbrt" => $"cbrt({currentVal})",
                "fact" => $"{currentVal}!",
                "percent" => $"({currentVal})/100",
                "abs" => $"abs({currentVal})",
                "exp" => $"exp({currentVal})",
                "pow10" => $"10^({currentVal})",
                "pow2" => $"2^({currentVal})",
                "log" => $"log({currentVal})",
                "log2" => $"log2({currentVal})",
                "ln" => $"ln({currentVal})",
                "floor" => $"floor({currentVal})",
                "ceil" => $"ceil({currentVal})",
                "sin" => $"sin({currentVal})",
                "cos" => $"cos({currentVal})",
                "tan" => $"tan({currentVal})",
                "asin" => $"asin({currentVal})",
                "acos" => $"acos({currentVal})",
                "atan" => $"atan({currentVal})",
                "sec" => $"sec({currentVal})",
                "csc" => $"csc({currentVal})",
                "cot" => $"cot({currentVal})",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(expr))
            {
                var result = _evaluator.Evaluate(expr);
                if (result.IsSuccess)
                {
                    Display = result.DisplayValue;
                    HasDecimalPoint = Display.Contains('.');
                    CurrentState = CalculatorState.InputOperand;
                    RecordTransition(fromState, CalculatorInputType.UnaryFunction, func, CurrentState, $"Immediate function '{func}' computed: {result.DisplayValue}");
                    return true;
                }
                else
                {
                    Display = result.DisplayValue;
                    CurrentState = CalculatorState.Error;
                    RecordTransition(fromState, CalculatorInputType.UnaryFunction, func, CurrentState, $"Immediate function '{func}' failed: {result.ErrorMessage}");
                    return false;
                }
            }
        }

        return false;
    }

    public bool ToggleSign()
    {
        if (CurrentState == CalculatorState.Error) return false;

        var fromState = CurrentState;

        if (Display.StartsWith("-"))
        {
            Display = Display[1..];
        }
        else if (Display != "0")
        {
            Display = "-" + Display;
        }

        RecordTransition(fromState, CalculatorInputType.ToggleSign, "±", CurrentState, $"Toggled sign to {Display}");
        return true;
    }

    public bool Backspace()
    {
        if (CurrentState != CalculatorState.InputOperand) return false;

        var fromState = CurrentState;

        if (Display.Length > 1)
        {
            if (Display[^1] == '.') HasDecimalPoint = false;
            Display = Display[..^1];
            if (Display == "-") Display = "0";
        }
        else
        {
            Display = "0";
            CurrentState = CalculatorState.Start;
        }

        RecordTransition(fromState, CalculatorInputType.Backspace, "⌫", CurrentState, $"Backspace to '{Display}'");
        return true;
    }

    public void ClearEntry()
    {
        var fromState = CurrentState;
        Display = "0";
        HasDecimalPoint = false;
        if (CurrentState == CalculatorState.InputOperand)
        {
            CurrentState = CalculatorState.OperatorEntered;
        }
        RecordTransition(fromState, CalculatorInputType.ClearEntry, "CE", CurrentState, "Cleared entry");
    }

    public CalculationResult Evaluate()
    {
        var fromState = CurrentState;

        if (CurrentState == CalculatorState.Error)
        {
            return CalculationResult.Fail("Error state. Press C to reset.");
        }

        // Repeated Equals Support (e.g. 5 + 2 = 7, then = -> 9, = -> 11)
        if (CurrentState == CalculatorState.Evaluated)
        {
            if (!string.IsNullOrEmpty(_lastOperator) && !string.IsNullOrEmpty(_lastOperand))
            {
                string repeatExpr = $"{Display} {_lastOperator} {_lastOperand}";
                var repeatRes = _evaluator.Evaluate(repeatExpr);

                if (repeatRes.IsSuccess)
                {
                    Display = repeatRes.DisplayValue;
                    ExpressionBuffer = $"{repeatExpr} =";
                    CurrentState = CalculatorState.Evaluated;
                    OpenParenCount = 0;
                    HasDecimalPoint = Display.Contains('.');
                    RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Repeated calculation: {repeatExpr} = {Display}");
                }
                else
                {
                    Display = repeatRes.DisplayValue;
                    CurrentState = CalculatorState.Error;
                    RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Repeated calculation failed: {repeatRes.ErrorMessage}");
                }

                return repeatRes;
            }

            // If no binary operator was saved (e.g. 5 = =), safely retain display
            RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Re-confirmed evaluation: {Display}");
            double.TryParse(Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double keepVal);
            return CalculationResult.Ok(keepVal);
        }

        // Build complete expression
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(ExpressionBuffer))
        {
            sb.Append(ExpressionBuffer);
            if (CurrentState == CalculatorState.InputOperand)
            {
                _lastOperator = ExtractLastOperator(ExpressionBuffer);
                _lastOperand = Display;
                sb.Append(Display);
            }
            else if (CurrentState == CalculatorState.OperatorEntered)
            {
                // Auto-complete dangling operator (e.g. 5 + = -> 5 + 5 = 10)
                _lastOperator = ExtractLastOperator(ExpressionBuffer);
                _lastOperand = Display;
                sb.Append(Display);
            }
        }
        else
        {
            sb.Append(Display);
        }

        // Auto-close any unclosed parentheses
        for (int i = 0; i < OpenParenCount; i++)
        {
            sb.Append(')');
        }

        string fullExpr = sb.ToString();
        var result = _evaluator.Evaluate(fullExpr);

        if (result.IsSuccess)
        {
            Display = result.DisplayValue;
            ExpressionBuffer = fullExpr + " =";
            CurrentState = CalculatorState.Evaluated;
            OpenParenCount = 0;
            HasDecimalPoint = Display.Contains('.');
            RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Successfully evaluated: {fullExpr} = {Display}");
        }
        else
        {
            Display = result.DisplayValue;
            CurrentState = CalculatorState.Error;
            RecordTransition(fromState, CalculatorInputType.Equals, "=", CurrentState, $"Evaluation failed: {result.ErrorMessage}");
        }

        return result;
    }

    private static string? ExtractLastOperator(string buffer)
    {
        if (string.IsNullOrWhiteSpace(buffer)) return null;
        string[] parts = buffer.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (parts[i] is "+" or "-" or "*" or "/" or "%" or "^")
            {
                return parts[i];
            }
        }
        return null;
    }

    public void ClearTransitionLog()
    {
        _transitionLog.Clear();
        _stepCounter = 0;
        RecordTransition(CurrentState, CalculatorInputType.Clear, "LOG_CLEARED", CurrentState, "Transition log cleared");
    }

    public bool ReplayToStep(int stepIndex)
    {
        var record = _transitionLog.FirstOrDefault(r => r.StepIndex == stepIndex);
        if (record == null) return false;

        Display = record.DisplaySnapshot;
        ExpressionBuffer = record.ExpressionSnapshot;
        CurrentState = record.ToState;
        HasDecimalPoint = Display.Contains('.');
        return true;
    }

    private void RecordTransition(CalculatorState from, CalculatorInputType inputType, string val, CalculatorState to, string desc)
    {
        _stepCounter++;
        _transitionLog.Add(new StateTransitionRecord
        {
            StepIndex = _stepCounter,
            FromState = from,
            InputType = inputType,
            InputValue = val,
            ToState = to,
            ActionDescription = desc,
            DisplaySnapshot = Display,
            ExpressionSnapshot = ExpressionBuffer,
            Timestamp = DateTime.Now
        });
    }
}
