using System.Text;
using Calculator.Core.Enums;
using Calculator.Core.Models;
using Calculator.Core.Parser;

namespace Calculator.Core.Services;

public class AutomataBuilderService
{
    private const double NodeRadius = 41.0;
    private readonly ExpressionEvaluator _evaluator = new();

    public AutomataGraph BuildDfaForExpression(string rawExpression, CalculatorState currentState, string? resultText = null)
    {
        string cleanExpr = SanitizeExpression(rawExpression);
        var tokens = ExtractTokens(cleanExpr);

        string evalResult = !string.IsNullOrWhiteSpace(resultText)
            ? resultText
            : EvaluateExpression(cleanExpr);

        var graph = new AutomataGraph
        {
            Title = $"DFA Nhận dạng & Tính toán Phép tính: {cleanExpr}",
            ModeBadge = "DFA (Biểu thức hiện hành)",
            FormalDefinition = $"M = (Q, Σ, δ, S₀, F) | Phép tính: {cleanExpr} = {evalResult}",
            Description = $"Otomát hữu hạn xác định (DFA) được tự động sinh tương ứng với phép tính '{cleanExpr}'. Mỗi trạng thái Sᵢ đại diện cho bước tiêu thụ token tiếp theo, đảm bảo tính đơn định tuyệt đối.",
            ActiveStateName = $"● Phép tính: {cleanExpr} | Trạng thái FSM: {currentState}"
        };

        var nodes = new List<AutomataNode>();
        var edges = new List<AutomataEdge>();

        // S0: Initial State
        nodes.Add(new AutomataNode
        {
            Id = "S0",
            StateKey = "Start",
            Title = "S0: START",
            Subtitle = "Khởi tạo",
            X = 80,
            Y = 180,
            IsStart = true,
            IsActive = (currentState == CalculatorState.Start)
        });

        double currentX = 80;
        double currentY = 180;
        double xStep = 165;

        // Create sequential states for each token in the calculation
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            int stateIdx = i + 1;
            string stateId = $"S{stateIdx}";
            currentX += xStep;

            string displayToken = token.Text == "neg" ? "-" : token.Text;
            string category = GetTokenCategory(token);
            bool isActiveState = (i == tokens.Count - 1 && currentState != CalculatorState.Start && currentState != CalculatorState.Error);

            nodes.Add(new AutomataNode
            {
                Id = stateId,
                StateKey = stateId,
                Title = $"{stateId}: [{displayToken}]",
                Subtitle = category,
                X = currentX,
                Y = currentY,
                IsActive = isActiveState
            });

            // Transition edge from previous state S_{i-1} to S_i
            var fromNode = nodes[i];
            var toNode = nodes[i + 1];
            edges.Add(CreateEdge(fromNode, toNode, displayToken, isLoop: false, isActive: isActiveState));
        }

        // S_Accept: Evaluating State when calculation completes with '='
        currentX += xStep;
        string acceptId = $"S{tokens.Count + 1}";
        var acceptNode = new AutomataNode
        {
            Id = acceptId,
            StateKey = "Evaluated",
            Title = $"SEVAL: [{evalResult}]",
            Subtitle = "Kết quả",
            X = currentX,
            Y = currentY,
            IsAccepting = true,
            IsActive = (currentState == CalculatorState.Evaluated)
        };
        nodes.Add(acceptNode);

        // Edge from last token to S_Accept on '='
        var lastTokenNode = nodes[^2];
        edges.Add(CreateEdge(lastTokenNode, acceptNode, "=", isLoop: false, isActive: currentState == CalculatorState.Evaluated));

        // Self-loop on accept node for repeated '=' calculations
        edges.Add(CreateEdge(acceptNode, acceptNode, "= (lặp)", isLoop: true, isActive: false));

        // S_Trap / Error node placed below middle
        double trapX = Math.Max(260, currentX / 2.0);
        double trapY = 330;
        var trapNode = new AutomataNode
        {
            Id = "S_TRAP",
            StateKey = "Error",
            Title = "STRAP: ERR",
            Subtitle = "Bẫy lỗi cú pháp",
            X = trapX,
            Y = trapY,
            IsError = true,
            IsActive = (currentState == CalculatorState.Error)
        };
        nodes.Add(trapNode);

        // Error edge for syntax errors (e.g. consecutive operators, missing operand)
        if (nodes.Count > 2)
        {
            // Connect from an operator state if available, or mid-state
            var opNode = nodes.FirstOrDefault(n => n.Subtitle.StartsWith("Toán tử")) ?? nodes[Math.Min(2, nodes.Count - 3)];
            edges.Add(CreateEdge(opNode, trapNode, "Toán tử sai (vd: lặp *, /)", isLoop: false, isActive: currentState == CalculatorState.Error));
        }

        graph.Nodes = nodes;
        graph.Edges = edges;
        graph.CanvasWidth = Math.Max(860, currentX + 140);
        graph.CanvasHeight = 440;

        return graph;
    }

    public AutomataGraph BuildNfaForExpression(string rawExpression)
    {
        string cleanExpr = SanitizeExpression(rawExpression);
        var tokens = ExtractTokens(cleanExpr);

        var graph = new AutomataGraph
        {
            Title = $"NFA Nhận dạng Cú pháp Phép tính: {cleanExpr}",
            ModeBadge = "NFA (Bước chuyển ε)",
            FormalDefinition = $"M = (Q, Σ, δ, q₀, F) | Phép tính: {cleanExpr} với bước chuyển ε",
            Description = $"Otomát hữu hạn bất định (NFA) theo mô hình Thompson cho phép tính '{cleanExpr}'. Sử dụng các bước chuyển ε (epsilon) để phân nhánh không tiền định giữa các toán tử và toán hạng theo mức ưu tiên cú pháp.",
            ActiveStateName = $"● Phép tính: {cleanExpr} | Mô hình Thompson NFA"
        };

        var nodes = new List<AutomataNode>();
        var edges = new List<AutomataEdge>();

        // q0: Start
        nodes.Add(new AutomataNode
        {
            Id = "q0",
            StateKey = "q0",
            Title = "q0: START",
            Subtitle = "Khởi tạo NFA",
            X = 80,
            Y = 180,
            IsStart = true
        });

        double currentX = 80;
        double currentY = 180;
        double xStep = 155;

        int nodeCounter = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            string displayToken = token.Text == "neg" ? "-" : token.Text;

            // 1. Epsilon transition state
            nodeCounter++;
            currentX += xStep;
            string epsNodeId = $"q{nodeCounter}";
            nodes.Add(new AutomataNode
            {
                Id = epsNodeId,
                StateKey = epsNodeId,
                Title = $"{epsNodeId}: (ε)",
                Subtitle = "Chuyển ε",
                X = currentX,
                Y = currentY
            });

            var prevNode = nodes[^2];
            var currNode = nodes[^1];
            edges.Add(CreateEdge(prevNode, currNode, "ε", isLoop: false, isActive: false));

            // 2. Token consumption state
            nodeCounter++;
            currentX += xStep;
            string tokenNodeId = $"q{nodeCounter}";
            bool isLast = (i == tokens.Count - 1);

            nodes.Add(new AutomataNode
            {
                Id = tokenNodeId,
                StateKey = tokenNodeId,
                Title = $"{tokenNodeId}: [{displayToken}]",
                Subtitle = GetTokenCategory(token),
                X = currentX,
                Y = currentY,
                IsAccepting = isLast
            });

            var epsNode = nodes[^2];
            var tokNode = nodes[^1];
            edges.Add(CreateEdge(epsNode, tokNode, displayToken, isLoop: false, isActive: isLast));
        }

        graph.Nodes = nodes;
        graph.Edges = edges;
        graph.CanvasWidth = Math.Max(860, currentX + 140);
        graph.CanvasHeight = 420;

        return graph;
    }

    private static string SanitizeExpression(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "8 * 9";
        string s = raw.Trim();
        if (s.EndsWith('=')) s = s[..^1].Trim();
        if (string.IsNullOrWhiteSpace(s) || s == "0") return "8 * 9";
        return s;
    }

    private List<Token> ExtractTokens(string expr)
    {
        try
        {
            var tokens = _evaluator.Tokenize(expr);
            if (tokens.Count > 0) return tokens;
        }
        catch
        {
            // Fallback for incomplete or partial expressions
        }

        // Fallback simple tokenization on whitespace or standard ops
        var fallbackTokens = new List<Token>();
        string[] parts = expr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (double.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                fallbackTokens.Add(new Token(TokenType.Number, p, val));
            }
            else
            {
                fallbackTokens.Add(new Token(TokenType.BinaryOperator, p));
            }
        }

        if (fallbackTokens.Count == 0)
        {
            fallbackTokens.Add(new Token(TokenType.Number, "8", 8));
            fallbackTokens.Add(new Token(TokenType.BinaryOperator, "*"));
            fallbackTokens.Add(new Token(TokenType.Number, "9", 9));
        }

        return fallbackTokens;
    }

    private string EvaluateExpression(string expr)
    {
        var res = _evaluator.Evaluate(expr);
        return res.IsSuccess ? res.DisplayValue : "0";
    }

    private static string GetTokenCategory(Token token) => token.Type switch
    {
        TokenType.Number => "Số (Num)",
        TokenType.BinaryOperator => "Toán tử",
        TokenType.UnaryOperator => "Toán tử âm (-)",
        TokenType.Function => "Hàm số",
        TokenType.Constant => "Hằng số",
        TokenType.LeftParen => "Mở ngoặc '('",
        TokenType.RightParen => "Đóng ngoặc ')'",
        _ => "Ký hiệu"
    };

    private static AutomataEdge CreateEdge(AutomataNode from, AutomataNode to, string label, bool isLoop, bool isActive)
    {
        var edge = new AutomataEdge
        {
            FromId = from.Id,
            ToId = to.Id,
            Label = label,
            IsSelfLoop = isLoop,
            IsActive = isActive
        };

        if (isLoop)
        {
            edge.X1 = from.X - 16;
            edge.Y1 = from.Y - NodeRadius + 2;
            edge.X2 = from.X + 16;
            edge.Y2 = from.Y - NodeRadius + 2;

            edge.LabelX = from.X - 22;
            edge.LabelY = from.Y - NodeRadius - 32;

            edge.ArrowTipX = edge.X2;
            edge.ArrowTipY = edge.Y2;
            edge.ArrowLeftX = edge.X2 + 5;
            edge.ArrowLeftY = edge.Y2 - 8;
            edge.ArrowRightX = edge.X2 - 4;
            edge.ArrowRightY = edge.Y2 - 8;
            return edge;
        }

        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double dist = Math.Sqrt((dx * dx) + (dy * dy));
        if (dist < 1.0) dist = 1.0;

        double ux = dx / dist;
        double uy = dy / dist;

        edge.X1 = from.X + (ux * NodeRadius);
        edge.Y1 = from.Y + (uy * NodeRadius);
        edge.X2 = to.X - (ux * (NodeRadius + 4));
        edge.Y2 = to.Y - (uy * (NodeRadius + 4));

        double perpX = -uy * 16;
        double perpY = ux * 16;

        // Position label cleanly
        if (Math.Abs(dy) < 30)
        {
            // For horizontal lines, place label safely above the arrow line
            edge.LabelX = ((edge.X1 + edge.X2) / 2.0) - 16;
            edge.LabelY = ((edge.Y1 + edge.Y2) / 2.0) - 22;
        }
        else
        {
            // For diagonal lines, offset perpendicularly
            edge.LabelX = ((edge.X1 + edge.X2) / 2.0) + perpX - 25;
            edge.LabelY = ((edge.Y1 + edge.Y2) / 2.0) + perpY - 10;
        }

        const double arrowLength = 9.0;
        const double arrowWidth = 5.0;

        edge.ArrowTipX = edge.X2;
        edge.ArrowTipY = edge.Y2;

        double backX = edge.ArrowTipX - (ux * arrowLength);
        double backY = edge.ArrowTipY - (uy * arrowLength);

        edge.ArrowLeftX = backX + (-uy * arrowWidth);
        edge.ArrowLeftY = backY + (ux * arrowWidth);

        edge.ArrowRightX = backX - (-uy * arrowWidth);
        edge.ArrowRightY = backY - (ux * arrowWidth);

        return edge;
    }
}
