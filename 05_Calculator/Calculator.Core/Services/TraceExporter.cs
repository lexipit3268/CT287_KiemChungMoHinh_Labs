using System.Text;
using System.Text.Json;
using Calculator.Core.Enums;
using Calculator.Core.Models;

namespace Calculator.Core.Services;

public static class TraceExporter
{
    public static string ExportGraphvizDot(CalculatorState activeState)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph CalculatorAutomaton {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [fontname=\"Segoe UI\", shape=ellipse, style=filled, fillcolor=\"#161B26\", fontcolor=\"#F8FAFC\", color=\"#333D52\"];");
        sb.AppendLine("    edge [fontname=\"Segoe UI\", fontsize=10, fontcolor=\"#94A3B8\", color=\"#475569\"];");
        sb.AppendLine();

        // States definitions
        var states = new[]
        {
            ("S0", "S0: Start\\n(Display='0')"),
            ("S1", "S1: InputOperand\\n(Number Entry)"),
            ("S2", "S2: OperatorEntered\\n(+,-,*,/,^,%)"),
            ("S3", "S3: OpenParen\\n('(' Entered)"),
            ("S4", "S4: CloseParen\\n(')' Entered)"),
            ("S5", "S5: Function\\n(sin,cos,log...)"),
            ("S6", "S6: Evaluated\\n(Result Displayed)"),
            ("S7", "S7: Error\\n(DivByZero/Syntax)")
        };

        foreach (var (id, label) in states)
        {
            bool isActive = id switch
            {
                "S0" => activeState == CalculatorState.Start,
                "S1" => activeState == CalculatorState.InputOperand,
                "S2" => activeState == CalculatorState.OperatorEntered,
                "S3" => activeState == CalculatorState.OpenParenEntered,
                "S4" => activeState == CalculatorState.CloseParenEntered,
                "S5" => activeState == CalculatorState.FunctionEntered,
                "S6" => activeState == CalculatorState.Evaluated,
                "S7" => activeState == CalculatorState.Error,
                _ => false
            };

            if (isActive)
            {
                sb.AppendLine($"    {id} [label=\"{label}\", fillcolor=\"#1f5ca9\", fontcolor=\"#FFFFFF\", color=\"#4ea2ff\", penwidth=2.5];");
            }
            else
            {
                sb.AppendLine($"    {id} [label=\"{label}\"];");
            }
        }

        sb.AppendLine();
        sb.AppendLine("    // Transitions (delta)");
        sb.AppendLine("    S0 -> S1 [label=\"Digit / .\"];");
        sb.AppendLine("    S0 -> S3 [label=\"'('\"];");
        sb.AppendLine("    S0 -> S5 [label=\"Func(\"];");
        sb.AppendLine();
        sb.AppendLine("    S1 -> S1 [label=\"Digit / .\"];");
        sb.AppendLine("    S1 -> S2 [label=\"BinaryOp\"];");
        sb.AppendLine("    S1 -> S4 [label=\"')' [guard: count>0]\"];");
        sb.AppendLine("    S1 -> S6 [label=\"'='\"];");
        sb.AppendLine("    S1 -> S7 [label=\"DivByZero\"];");
        sb.AppendLine();
        sb.AppendLine("    S2 -> S1 [label=\"Digit / .\"];");
        sb.AppendLine("    S2 -> S2 [label=\"ReplaceOp\"];");
        sb.AppendLine("    S2 -> S3 [label=\"'('\"];");
        sb.AppendLine();
        sb.AppendLine("    S3 -> S1 [label=\"Digit\"];");
        sb.AppendLine("    S3 -> S3 [label=\"'('\"];");
        sb.AppendLine();
        sb.AppendLine("    S4 -> S2 [label=\"BinaryOp\"];");
        sb.AppendLine("    S4 -> S4 [label=\"')'\"];");
        sb.AppendLine("    S4 -> S6 [label=\"'='\"];");
        sb.AppendLine();
        sb.AppendLine("    S5 -> S1 [label=\"Digit\"];");
        sb.AppendLine("    S5 -> S3 [label=\"'('\"];");
        sb.AppendLine();
        sb.AppendLine("    S6 -> S1 [label=\"New Digit\"];");
        sb.AppendLine("    S6 -> S2 [label=\"Chain Op\"];");
        sb.AppendLine();
        sb.AppendLine("    // Reset from any state");
        sb.AppendLine("    S1 -> S0 [label=\"Clear/Reset\", style=dashed, color=\"#64748B\"];");
        sb.AppendLine("    S2 -> S0 [label=\"Clear/Reset\", style=dashed, color=\"#64748B\"];");
        sb.AppendLine("    S6 -> S0 [label=\"Clear/Reset\", style=dashed, color=\"#64748B\"];");
        sb.AppendLine("    S7 -> S0 [label=\"Clear/Reset\", style=dashed, color=\"#EF4444\"];");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string ExportJsonTrace(IEnumerable<StateTransitionRecord> trace)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(trace, options);
    }
}
