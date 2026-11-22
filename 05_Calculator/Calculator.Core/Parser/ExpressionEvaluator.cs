using System.Globalization;
using System.Text;
using Calculator.Core.Enums;
using Calculator.Core.Models;

namespace Calculator.Core.Parser;

public class ExpressionEvaluator
{
    private static readonly HashSet<string> StandardFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "sin", "cos", "tan", "sec", "csc", "cot",
        "asin", "acos", "atan",
        "sinh", "cosh", "tanh",
        "log", "ln", "log10", "log2",
        "sqrt", "cbrt", "abs", "exp",
        "sqr", "cube", "recip", "fact",
        "floor", "ceil"
    };

    public AngleUnit AngleMode { get; set; } = AngleUnit.Degree;

    public CalculationResult Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return CalculationResult.Ok(0);
        }

        try
        {
            // Strip any trailing '=' or whitespace before parsing
            expression = expression.Trim().TrimEnd('=').Trim();
            if (string.IsNullOrWhiteSpace(expression))
            {
                return CalculationResult.Ok(0);
            }

            var tokens = Tokenize(expression);
            if (tokens.Count == 0) return CalculationResult.Ok(0);

            var rpn = ShuntingYard(tokens);
            double result = EvaluateRpn(rpn);

            return CalculationResult.Ok(result);
        }
        catch (DivideByZeroException ex)
        {
            return CalculationResult.Fail($"Error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return CalculationResult.Fail($"Error: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return CalculationResult.Fail($"Syntax Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CalculationResult.Fail($"Error: {ex.Message}");
        }
    }

    public List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        int i = 0;
        int n = expr.Length;

        // Clean up common aliases (e.g. × to *, ÷ to /, π to pi)
        string s = expr
            .Replace("×", "*")
            .Replace("÷", "/")
            .Replace("−", "-")
            .Replace("π", "pi")
            .Replace("√", "sqrt");

        bool expectUnary = true;

        while (i < s.Length)
        {
            char c = s[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Check for number (digit or decimal point starting a number)
            if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                int start = i;
                bool hasDot = (c == '.');
                i++;
                while (i < s.Length && (char.IsDigit(s[i]) || (s[i] == '.' && !hasDot)))
                {
                    if (s[i] == '.') hasDot = true;
                    i++;
                }

                // Check for scientific notation (e.g., 1e-5, 2.5E+3)
                if (i < s.Length && (s[i] == 'e' || s[i] == 'E') && i + 1 < s.Length &&
                    (char.IsDigit(s[i + 1]) || ((s[i + 1] == '+' || s[i + 1] == '-') && i + 2 < s.Length && char.IsDigit(s[i + 2]))))
                {
                    i++;
                    if (s[i] == '+' || s[i] == '-') i++;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                }

                string numStr = s[start..i];
                if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                {
                    throw new InvalidOperationException($"Invalid number: {numStr}");
                }

                // Implicit multiplication check (e.g. 5( or 5pi or 5sin)
                if (!expectUnary && (tokens.Count > 0 && (tokens[^1].Type == TokenType.Number || tokens[^1].Type == TokenType.RightParen || tokens[^1].Type == TokenType.Constant)))
                {
                    tokens.Add(new Token(TokenType.BinaryOperator, "*", precedence: 2));
                }

                tokens.Add(new Token(TokenType.Number, numStr, val));
                expectUnary = false;
                continue;
            }

            // Check for words (functions, constants like pi, e)
            if (char.IsLetter(c))
            {
                int start = i;
                while (i < s.Length && char.IsLetterOrDigit(s[i]))
                {
                    i++;
                }
                string word = s[start..i].ToLowerInvariant();

                // Implicit multiplication before word/constant if preceded by number, constant, or right paren
                if (!expectUnary && tokens.Count > 0 &&
                    (tokens[^1].Type == TokenType.Number || tokens[^1].Type == TokenType.RightParen || tokens[^1].Type == TokenType.Constant))
                {
                    tokens.Add(new Token(TokenType.BinaryOperator, "*", precedence: 2));
                }

                if (word == "pi")
                {
                    tokens.Add(new Token(TokenType.Constant, "π", Math.PI));
                    expectUnary = false;
                }
                else if (word == "e")
                {
                    tokens.Add(new Token(TokenType.Constant, "e", Math.E));
                    expectUnary = false;
                }
                else if (StandardFunctions.Contains(word))
                {
                    tokens.Add(new Token(TokenType.Function, word, precedence: 6));
                    expectUnary = true;
                }
                else
                {
                    throw new InvalidOperationException($"Unknown function or symbol: '{word}'");
                }
                continue;
            }

            // Unary minus or binary minus
            if (c == '-')
            {
                if (expectUnary)
                {
                    // Unary negation operator (Precedence 4, right-associative)
                    tokens.Add(new Token(TokenType.UnaryOperator, "neg", precedence: 4, isRightAssociative: true, parameterCount: 1));
                    expectUnary = true;
                    i++;
                    continue;
                }
                else
                {
                    tokens.Add(new Token(TokenType.BinaryOperator, "-", precedence: 1));
                    expectUnary = true;
                    i++;
                    continue;
                }
            }

            // Unary plus is ignored
            if (c == '+' && expectUnary)
            {
                i++;
                continue;
            }

            // Binary operators
            if (c == '+')
            {
                tokens.Add(new Token(TokenType.BinaryOperator, "+", precedence: 1));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == '*' || c == '×')
            {
                tokens.Add(new Token(TokenType.BinaryOperator, "*", precedence: 2));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == '/' || c == '÷')
            {
                tokens.Add(new Token(TokenType.BinaryOperator, "/", precedence: 2));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == '%')
            {
                // Modulo operator (precedence 2)
                tokens.Add(new Token(TokenType.BinaryOperator, "%", precedence: 2));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == '^')
            {
                // Power (precedence 3, right associative: 2^3^2 = 2^(3^2) = 512)
                tokens.Add(new Token(TokenType.BinaryOperator, "^", precedence: 3, isRightAssociative: true));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == '!')
            {
                // Factorial (postfix unary operator, high precedence)
                tokens.Add(new Token(TokenType.UnaryOperator, "!", precedence: 5, isRightAssociative: false, parameterCount: 1));
                expectUnary = false;
                i++;
                continue;
            }

            // Parentheses
            if (c == '(')
            {
                // Implicit multiplication: e.g. 5(3+2) or )(
                if (!expectUnary && tokens.Count > 0 &&
                    (tokens[^1].Type == TokenType.Number || tokens[^1].Type == TokenType.RightParen || tokens[^1].Type == TokenType.Constant))
                {
                    tokens.Add(new Token(TokenType.BinaryOperator, "*", precedence: 2));
                }

                tokens.Add(new Token(TokenType.LeftParen, "("));
                expectUnary = true;
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")"));
                expectUnary = false;
                i++;
                continue;
            }

            // Safely skip any stray equals '=' character (e.g. from history replay or display buffer)
            if (c == '=')
            {
                i++;
                continue;
            }

            throw new InvalidOperationException($"Unexpected character: '{c}' at position {i}");
        }

        return tokens;
    }

    public List<Token> ShuntingYard(List<Token> tokens)
    {
        var outputQueue = new List<Token>();
        var operatorStack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Constant:
                    outputQueue.Add(token);
                    break;

                case TokenType.Function:
                    operatorStack.Push(token);
                    break;

                case TokenType.UnaryOperator:
                    if (token.Text == "!") // Postfix factorial operator
                    {
                        outputQueue.Add(token);
                    }
                    else
                    {
                        operatorStack.Push(token);
                    }
                    break;

                case TokenType.BinaryOperator:
                    while (operatorStack.Count > 0 && operatorStack.Peek().Type != TokenType.LeftParen)
                    {
                        var top = operatorStack.Peek();
                        bool shouldPop = false;

                        if (top.Type == TokenType.Function || top.Type == TokenType.UnaryOperator)
                        {
                            shouldPop = true;
                        }
                        else if (top.Precedence > token.Precedence)
                        {
                            shouldPop = true;
                        }
                        else if (top.Precedence == token.Precedence && !token.IsRightAssociative)
                        {
                            shouldPop = true;
                        }

                        if (shouldPop)
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        else
                        {
                            break;
                        }
                    }
                    operatorStack.Push(token);
                    break;

                case TokenType.LeftParen:
                    operatorStack.Push(token);
                    break;

                case TokenType.RightParen:
                    bool matched = false;
                    while (operatorStack.Count > 0)
                    {
                        var top = operatorStack.Pop();
                        if (top.Type == TokenType.LeftParen)
                        {
                            matched = true;
                            break;
                        }
                        outputQueue.Add(top);
                    }

                    if (!matched)
                    {
                        throw new InvalidOperationException("Mismatched closing parenthesis ')'");
                    }

                    // If a function precedes the left paren, pop it into output
                    if (operatorStack.Count > 0 && operatorStack.Peek().Type == TokenType.Function)
                    {
                        outputQueue.Add(operatorStack.Pop());
                    }
                    break;
            }
        }

        while (operatorStack.Count > 0)
        {
            var top = operatorStack.Pop();
            if (top.Type == TokenType.LeftParen || top.Type == TokenType.RightParen)
            {
                throw new InvalidOperationException("Mismatched opening parenthesis '('");
            }
            outputQueue.Add(top);
        }

        return outputQueue;
    }

    public double EvaluateRpn(List<Token> rpn)
    {
        var stack = new Stack<double>();

        foreach (var token in rpn)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    stack.Push(token.Value);
                    break;

                case TokenType.Constant:
                    stack.Push(token.Value);
                    break;

                case TokenType.UnaryOperator:
                    if (token.Text == "neg")
                    {
                        if (stack.Count < 1) throw new InvalidOperationException("Missing operand for negation");
                        stack.Push(-stack.Pop());
                    }
                    else if (token.Text == "!")
                    {
                        if (stack.Count < 1) throw new InvalidOperationException("Missing operand for factorial");
                        stack.Push(CalculateFactorial(stack.Pop()));
                    }
                    break;

                case TokenType.BinaryOperator:
                    if (stack.Count < 2) throw new InvalidOperationException($"Missing operand for operator '{token.Text}'");
                    double b = stack.Pop();
                    double a = stack.Pop();
                    stack.Push(ExecuteBinaryOp(token.Text, a, b));
                    break;

                case TokenType.Function:
                    if (stack.Count < 1) throw new InvalidOperationException($"Missing argument for function '{token.Text}'");
                    double arg = stack.Pop();
                    stack.Push(ExecuteFunction(token.Text, arg));
                    break;

                default:
                    throw new InvalidOperationException($"Unknown token in evaluation: {token.Text}");
            }
        }

        if (stack.Count != 1)
        {
            throw new InvalidOperationException("The expression is malformed (too many operands)");
        }

        return stack.Pop();
    }

    private double ExecuteBinaryOp(string op, double a, double b)
    {
        return op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => Math.Abs(b) < 1e-15 ? throw new DivideByZeroException("Cannot divide by zero") : a / b,
            "%" => Math.Abs(b) < 1e-15 ? throw new DivideByZeroException("Cannot modulo by zero") : a % b,
            "^" => Math.Pow(a, b),
            _ => throw new InvalidOperationException($"Unsupported binary operator: '{op}'")
        };
    }

    private double ExecuteFunction(string func, double arg)
    {
        double toRadians(double deg) => deg * Math.PI / 180.0;
        double toDegrees(double rad) => rad * 180.0 / Math.PI;

        string lowerFunc = func.ToLowerInvariant();
        double val = lowerFunc switch
        {
            "sin" => AngleMode == AngleUnit.Degree ? Math.Sin(toRadians(arg)) : Math.Sin(arg),
            "cos" => AngleMode == AngleUnit.Degree ? Math.Cos(toRadians(arg)) : Math.Cos(arg),
            "tan" => AngleMode == AngleUnit.Degree ?
                        (Math.Abs(arg % 180) == 90 ? throw new ArgumentException("Tan is undefined at 90° + k·180°") : Math.Tan(toRadians(arg))) :
                        Math.Tan(arg),
            "sec" => AngleMode == AngleUnit.Degree ? 1.0 / Math.Cos(toRadians(arg)) : 1.0 / Math.Cos(arg),
            "csc" => AngleMode == AngleUnit.Degree ? 1.0 / Math.Sin(toRadians(arg)) : 1.0 / Math.Sin(arg),
            "cot" => AngleMode == AngleUnit.Degree ? 1.0 / Math.Tan(toRadians(arg)) : 1.0 / Math.Tan(arg),
            "asin" => arg < -1.0 || arg > 1.0 ? throw new ArgumentException("Asin domain is [-1, 1]") :
                        (AngleMode == AngleUnit.Degree ? toDegrees(Math.Asin(arg)) : Math.Asin(arg)),
            "acos" => arg < -1.0 || arg > 1.0 ? throw new ArgumentException("Acos domain is [-1, 1]") :
                        (AngleMode == AngleUnit.Degree ? toDegrees(Math.Acos(arg)) : Math.Acos(arg)),
            "atan" => AngleMode == AngleUnit.Degree ? toDegrees(Math.Atan(arg)) : Math.Atan(arg),
            "sinh" => Math.Sinh(arg),
            "cosh" => Math.Cosh(arg),
            "tanh" => Math.Tanh(arg),
            "log" or "log10" => arg <= 0 ? throw new ArgumentException("Logarithm argument must be > 0") : Math.Log10(arg),
            "ln" => arg <= 0 ? throw new ArgumentException("Ln argument must be > 0") : Math.Log(arg),
            "log2" => arg <= 0 ? throw new ArgumentException("Log2 argument must be > 0") : Math.Log2(arg),
            "sqrt" => arg < 0 ? throw new ArgumentException("Cannot calculate square root of a negative number") : Math.Sqrt(arg),
            "cbrt" => Math.Cbrt(arg),
            "abs" => Math.Abs(arg),
            "exp" => Math.Exp(arg),
            "sqr" => arg * arg,
            "cube" => arg * arg * arg,
            "recip" => Math.Abs(arg) < 1e-15 ? throw new DivideByZeroException("Cannot divide by zero") : 1.0 / arg,
            "fact" => CalculateFactorial(arg),
            "floor" => Math.Floor(arg),
            "ceil" => Math.Ceiling(arg),
            _ => throw new InvalidOperationException($"Unsupported function '{func}'")
        };

        // Clean precision artifacts for trig in degrees
        if (AngleMode == AngleUnit.Degree && (lowerFunc == "sin" || lowerFunc == "cos" || lowerFunc == "tan"))
        {
            val = Math.Round(val, 14);
            if (Math.Abs(val) < 1e-14) val = 0;
        }

        return val;
    }

    private static double CalculateFactorial(double n)
    {
        if (n < 0 || n != Math.Floor(n))
        {
            throw new ArgumentException("Factorial is only defined for non-negative integers");
        }
        if (n > 170)
        {
            throw new OverflowException("Factorial overflow (> 170!)");
        }

        int intN = (int)n;
        double result = 1.0;
        for (int i = 2; i <= intN; i++)
        {
            result *= i;
        }
        return result;
    }
}
