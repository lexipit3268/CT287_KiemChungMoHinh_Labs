using System.Globalization;
using Calculator.Core.Models;
using Calculator.Core.Parser;

namespace Calculator.Core.Services;

public class ExpressionTreeBuilder
{
    private readonly ExpressionEvaluator _evaluator;
    private int _nodeCounter = 0;

    public ExpressionTreeBuilder(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator ?? new ExpressionEvaluator();
    }

    public ExpressionTreeGraph BuildGraph(string expression, bool fullParseTree = false)
    {
        string cleanExpr = expression.Trim();
        if (cleanExpr.EndsWith('='))
        {
            cleanExpr = cleanExpr[..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(cleanExpr))
        {
            cleanExpr = "0";
        }

        _nodeCounter = 0;
        ExpressionTreeNode rootNode;
        double evaluatedValue = 0;
        string rpnString = string.Empty;

        try
        {
            var res = _evaluator.Evaluate(cleanExpr);
            evaluatedValue = res.Value;

            var tokens = _evaluator.Tokenize(cleanExpr);
            var rpnTokens = _evaluator.ShuntingYard(tokens);
            rpnString = string.Join(" ", rpnTokens.Select(t => t.Text));

            if (fullParseTree)
            {
                var parser = new RecursiveDescentParser(cleanExpr, this);
                rootNode = parser.Parse();
            }
            else
            {
                rootNode = BuildAstFromRpn(rpnTokens);
            }
        }
        catch
        {
            rootNode = new ExpressionTreeNode
            {
                Id = "node_1",
                Label = cleanExpr,
                Type = ExpressionTreeNodeType.Number,
                Value = 0
            };
        }

        // Layout the tree
        var allNodes = new List<ExpressionTreeNode>();
        var allEdges = new List<ExpressionTreeEdge>();

        double maxX = ComputeLayout(rootNode, 40, 1, 90);
        double maxY = GetMaxDepth(rootNode) * 90 + 120;

        FlattenGraph(rootNode, allNodes, allEdges);

        return new ExpressionTreeGraph
        {
            Expression = cleanExpr,
            Result = CalculationResult.FormatNumber(evaluatedValue),
            Rpn = rpnString,
            Nodes = allNodes,
            Edges = allEdges,
            CanvasWidth = Math.Max(500, maxX + 80),
            CanvasHeight = Math.Max(350, maxY)
        };
    }

    private ExpressionTreeNode BuildAstFromRpn(List<Token> rpn)
    {
        var stack = new Stack<ExpressionTreeNode>();

        foreach (var token in rpn)
        {
            _nodeCounter++;
            var node = new ExpressionTreeNode
            {
                Id = $"ast_{_nodeCounter}",
                Label = token.Text
            };

            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Constant:
                    node.Type = ExpressionTreeNodeType.Number;
                    node.Value = token.Value;
                    stack.Push(node);
                    break;

                case TokenType.BinaryOperator:
                    node.Type = ExpressionTreeNodeType.Operator;
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        node.Children.Add(left);
                        node.Children.Add(right);
                    }
                    stack.Push(node);
                    break;

                case TokenType.Function:
                case TokenType.UnaryOperator:
                    node.Type = ExpressionTreeNodeType.Function;
                    if (stack.Count >= 1)
                    {
                        var child = stack.Pop();
                        node.Children.Add(child);
                    }
                    stack.Push(node);
                    break;
            }
        }

        return stack.Count > 0 ? stack.Pop() : new ExpressionTreeNode { Id = "ast_0", Label = "0" };
    }

    private double ComputeLayout(ExpressionTreeNode node, double startX, double level, double levelHeight = 90)
    {
        node.Y = level * levelHeight;

        if (node.Children.Count == 0)
        {
            node.X = startX + 35;
            return startX + 70;
        }

        double currentX = startX;
        var childMidPoints = new List<double>();

        foreach (var child in node.Children)
        {
            double nextX = ComputeLayout(child, currentX, level + 1, levelHeight);
            childMidPoints.Add(child.X);
            currentX = nextX;
        }

        node.X = (childMidPoints.First() + childMidPoints.Last()) / 2.0;
        return currentX;
    }

    private void FlattenGraph(ExpressionTreeNode node, List<ExpressionTreeNode> nodes, List<ExpressionTreeEdge> edges)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            edges.Add(new ExpressionTreeEdge
            {
                X1 = node.X,
                Y1 = node.Y + (node.Height / 2.0),
                X2 = child.X,
                Y2 = child.Y - (child.Height / 2.0)
            });
            FlattenGraph(child, nodes, edges);
        }
    }

    private int GetMaxDepth(ExpressionTreeNode node)
    {
        if (node.Children.Count == 0) return 1;
        return 1 + node.Children.Max(GetMaxDepth);
    }

    public string NextId()
    {
        _nodeCounter++;
        return $"n_{_nodeCounter}";
    }

    // Grammar V2 Recursive Descent Parser for Session 5 Parse Tree (E -> T ((+|-|*) T)*)
    private class RecursiveDescentParser
    {
        private readonly string _input;
        private readonly ExpressionTreeBuilder _builder;
        private int _pos = 0;

        public RecursiveDescentParser(string input, ExpressionTreeBuilder builder)
        {
            _input = input;
            _builder = builder;
        }

        public ExpressionTreeNode Parse()
        {
            SkipWhitespace();
            var eNode = ParseExpression();
            return eNode;
        }

        private void SkipWhitespace()
        {
            while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            {
                _pos++;
            }
        }

        // E -> T ((+ | -) T)*
        private ExpressionTreeNode ParseExpression()
        {
            var eNode = new ExpressionTreeNode
            {
                Id = _builder.NextId(),
                Label = "E",
                Type = ExpressionTreeNodeType.Expression
            };

            var tNode = ParseTerm();
            eNode.Children.Add(tNode);
            SkipWhitespace();

            while (_pos < _input.Length && (_input[_pos] == '+' || _input[_pos] == '-'))
            {
                char op = _input[_pos++];
                var opNode = new ExpressionTreeNode
                {
                    Id = _builder.NextId(),
                    Label = op.ToString(),
                    Type = ExpressionTreeNodeType.Operator
                };
                eNode.Children.Add(opNode);

                SkipWhitespace();
                var nextT = ParseTerm();
                eNode.Children.Add(nextT);
                SkipWhitespace();
            }

            return eNode;
        }

        // T -> F ((* | / | %) F)*
        private ExpressionTreeNode ParseTerm()
        {
            var tNode = new ExpressionTreeNode
            {
                Id = _builder.NextId(),
                Label = "T",
                Type = ExpressionTreeNodeType.Term
            };

            var fNode = ParseFactor();
            tNode.Children.Add(fNode);
            SkipWhitespace();

            while (_pos < _input.Length && (_input[_pos] == '*' || _input[_pos] == '/' || _input[_pos] == '×' || _input[_pos] == '÷' || _input[_pos] == '%'))
            {
                char op = _input[_pos++];
                var opNode = new ExpressionTreeNode
                {
                    Id = _builder.NextId(),
                    Label = op == '×' ? "*" : (op == '÷' ? "/" : op.ToString()),
                    Type = ExpressionTreeNodeType.Operator
                };
                tNode.Children.Add(opNode);

                SkipWhitespace();
                var nextF = ParseFactor();
                tNode.Children.Add(nextF);
                SkipWhitespace();
            }

            return tNode;
        }

        // F -> (E) | id | func(E)
        private ExpressionTreeNode ParseFactor()
        {
            var fNode = new ExpressionTreeNode
            {
                Id = _builder.NextId(),
                Label = "F",
                Type = ExpressionTreeNodeType.Factor
            };

            SkipWhitespace();
            if (_pos >= _input.Length) return fNode;

            if (_input[_pos] == '(')
            {
                _pos++; // skip '('
                var openParen = new ExpressionTreeNode
                {
                    Id = _builder.NextId(),
                    Label = "(",
                    Type = ExpressionTreeNodeType.Parenthesis
                };
                fNode.Children.Add(openParen);

                var innerE = ParseExpression();
                fNode.Children.Add(innerE);

                SkipWhitespace();
                if (_pos < _input.Length && _input[_pos] == ')')
                {
                    _pos++; // skip ')'
                    var closeParen = new ExpressionTreeNode
                    {
                        Id = _builder.NextId(),
                        Label = ")",
                        Type = ExpressionTreeNodeType.Parenthesis
                    };
                    fNode.Children.Add(closeParen);
                }
            }
            else if (char.IsLetter(_input[_pos]))
            {
                // Word or function
                int start = _pos;
                while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos])))
                {
                    _pos++;
                }
                string word = _input[start.._pos];
                var wordNode = new ExpressionTreeNode
                {
                    Id = _builder.NextId(),
                    Label = word,
                    Type = ExpressionTreeNodeType.Function
                };
                fNode.Children.Add(wordNode);

                SkipWhitespace();
                if (_pos < _input.Length && _input[_pos] == '(')
                {
                    _pos++; // skip '('
                    var innerE = ParseExpression();
                    fNode.Children.Add(innerE);
                    SkipWhitespace();
                    if (_pos < _input.Length && _input[_pos] == ')')
                    {
                        _pos++;
                    }
                }
            }
            else
            {
                // Number
                int start = _pos;
                if (_input[_pos] == '-') _pos++;
                while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
                {
                    _pos++;
                }
                string numStr = _input[start.._pos];
                double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val);

                var idNode = new ExpressionTreeNode
                {
                    Id = _builder.NextId(),
                    Label = numStr,
                    Type = ExpressionTreeNodeType.Number,
                    Value = val
                };
                fNode.Children.Add(idNode);
            }

            return fNode;
        }
    }
}
