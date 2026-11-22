using System.Text;
using Calculator.Core.Models;
using Calculator.Core.Parser;

namespace Calculator.Core.Services;

public class AstVisualizerService
{
    private readonly ExpressionEvaluator _evaluator;

    public AstVisualizerService(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator ?? new ExpressionEvaluator();
    }

    public List<ShuntingYardTraceStep> GenerateShuntingYardTrace(string expression)
    {
        var steps = new List<ShuntingYardTraceStep>();
        if (string.IsNullOrWhiteSpace(expression)) return steps;

        try
        {
            // Strip trailing '=' if present
            string cleanExpr = expression.Trim();
            if (cleanExpr.EndsWith('='))
            {
                cleanExpr = cleanExpr[..^1].Trim();
            }

            var tokens = _evaluator.Tokenize(cleanExpr);
            var operatorStack = new Stack<Token>();
            var outputQueue = new List<Token>();
            int stepIndex = 0;

            steps.Add(new ShuntingYardTraceStep
            {
                Step = ++stepIndex,
                Token = "START",
                Action = $"Tokenized {tokens.Count} tokens from expression: '{cleanExpr}'",
                OperatorStack = "[]",
                OutputQueue = "[]"
            });

            foreach (var token in tokens)
            {
                stepIndex++;
                string action;

                switch (token.Type)
                {
                    case TokenType.Number:
                    case TokenType.Constant:
                        outputQueue.Add(token);
                        action = $"Add operand '{token.Text}' to output queue";
                        break;

                    case TokenType.Function:
                        operatorStack.Push(token);
                        action = $"Push function '{token.Text}' to operator stack";
                        break;

                    case TokenType.LeftParen:
                        operatorStack.Push(token);
                        action = "Push left paren '(' to operator stack";
                        break;

                    case TokenType.RightParen:
                        action = "Pop operators to output until matching '(' is found";
                        while (operatorStack.Count > 0 && operatorStack.Peek().Type != TokenType.LeftParen)
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        if (operatorStack.Count > 0 && operatorStack.Peek().Type == TokenType.LeftParen)
                        {
                            operatorStack.Pop(); // Discard '('
                        }
                        if (operatorStack.Count > 0 && operatorStack.Peek().Type == TokenType.Function)
                        {
                            outputQueue.Add(operatorStack.Pop());
                        }
                        break;

                    case TokenType.UnaryOperator:
                        if (token.Text == "!")
                        {
                            outputQueue.Add(token);
                            action = "Add postfix factorial '!' to output queue";
                        }
                        else
                        {
                            operatorStack.Push(token);
                            action = $"Push unary operator '{token.Text}' to stack";
                        }
                        break;

                    case TokenType.BinaryOperator:
                        action = $"Process binary operator '{token.Text}' (Precedence: {token.Precedence})";
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

                    default:
                        action = $"Read token '{token.Text}'";
                        break;
                }

                steps.Add(new ShuntingYardTraceStep
                {
                    Step = stepIndex,
                    Token = token.Text,
                    Action = action,
                    OperatorStack = $"[{string.Join(", ", operatorStack.Select(t => t.Text))}]",
                    OutputQueue = $"[{string.Join(" ", outputQueue.Select(t => t.Text))}]"
                });
            }

            // Pop remaining operators
            while (operatorStack.Count > 0)
            {
                var top = operatorStack.Pop();
                if (top.Type != TokenType.LeftParen)
                {
                    outputQueue.Add(top);
                    steps.Add(new ShuntingYardTraceStep
                    {
                        Step = ++stepIndex,
                        Token = top.Text,
                        Action = $"Pop remaining operator '{top.Text}' from stack to output queue",
                        OperatorStack = $"[{string.Join(", ", operatorStack.Select(t => t.Text))}]",
                        OutputQueue = $"[{string.Join(" ", outputQueue.Select(t => t.Text))}]"
                    });
                }
            }

            steps.Add(new ShuntingYardTraceStep
            {
                Step = ++stepIndex,
                Token = "COMPLETE",
                Action = $"Shunting-Yard finished. Reverse Polish Notation (RPN): {string.Join(" ", outputQueue.Select(t => t.Text))}",
                OperatorStack = "[]",
                OutputQueue = $"[{string.Join(" ", outputQueue.Select(t => t.Text))}]"
            });
        }
        catch (Exception ex)
        {
            steps.Add(new ShuntingYardTraceStep
            {
                Step = steps.Count + 1,
                Token = "ERROR",
                Action = $"Trace generation error: {ex.Message}",
                OperatorStack = "[]",
                OutputQueue = "[]"
            });
        }

        return steps;
    }

    public AstNode? BuildAst(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;

        try
        {
            string cleanExpr = expression.Trim();
            if (cleanExpr.EndsWith('='))
            {
                cleanExpr = cleanExpr[..^1].Trim();
            }

            var tokens = _evaluator.Tokenize(cleanExpr);
            var rpn = _evaluator.ShuntingYard(tokens);

            var stack = new Stack<AstNode>();

            foreach (var token in rpn)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                    case TokenType.Constant:
                        stack.Push(new AstNode
                        {
                            Value = token.Text,
                            NodeType = "Operand",
                            Depth = 0
                        });
                        break;

                    case TokenType.BinaryOperator:
                        if (stack.Count >= 2)
                        {
                            var right = stack.Pop();
                            var left = stack.Pop();
                            int maxChildDepth = Math.Max(left.Depth, right.Depth);

                            stack.Push(new AstNode
                            {
                                Value = token.Text,
                                NodeType = "BinaryOperator",
                                Left = left,
                                Right = right,
                                Depth = maxChildDepth + 1
                            });
                        }
                        break;

                    case TokenType.UnaryOperator:
                    case TokenType.Function:
                        if (stack.Count >= 1)
                        {
                            var child = stack.Pop();
                            stack.Push(new AstNode
                            {
                                Value = token.Text,
                                NodeType = token.Type == TokenType.Function ? "Function" : "UnaryOperator",
                                Left = child,
                                Depth = child.Depth + 1
                            });
                        }
                        break;
                }
            }

            return stack.Count > 0 ? stack.Pop() : null;
        }
        catch
        {
            return null;
        }
    }

    public string FormatAstTree(AstNode? root)
    {
        if (root == null) return "(Empty AST)";
        var sb = new StringBuilder();
        RenderNode(root, sb, "", true);
        return sb.ToString();
    }

    private void RenderNode(AstNode node, StringBuilder sb, string indent, bool isLast)
    {
        sb.Append(indent);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine($"{node.Value} [{node.NodeType}]");

        var children = new List<AstNode>();
        if (node.Left != null) children.Add(node.Left);
        if (node.Right != null) children.Add(node.Right);

        for (int i = 0; i < children.Count; i++)
        {
            string childIndent = indent + (isLast ? "    " : "│   ");
            RenderNode(children[i], sb, childIndent, i == children.Count - 1);
        }
    }
}
