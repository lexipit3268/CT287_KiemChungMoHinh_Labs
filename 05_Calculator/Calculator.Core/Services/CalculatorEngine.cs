using Calculator.Core.Enums;
using Calculator.Core.Models;
using Calculator.Core.StateMachine;

namespace Calculator.Core.Services;

public class CalculatorEngine
{
    public CalculatorStateMachine StateMachine { get; }
    public IMemoryService Memory { get; }
    public IHistoryService History { get; }

    public CalculatorMode Mode { get; set; } = CalculatorMode.Standard;

    public AngleUnit AngleMode
    {
        get => StateMachine.AngleMode;
        set => StateMachine.AngleMode = value;
    }

    public CalculatorEngine(CalculatorStateMachine? stateMachine = null, IMemoryService? memory = null, IHistoryService? history = null)
    {
        StateMachine = stateMachine ?? new CalculatorStateMachine();
        Memory = memory ?? new MemoryService();
        History = history ?? new HistoryService();
    }

    public CalculationResult EvaluateAndRecord()
    {
        string beforeBuffer = StateMachine.ExpressionBuffer;
        string beforeDisplay = StateMachine.Display;
        var result = StateMachine.Evaluate();

        if (result.IsSuccess)
        {
            string recordedExpr = StateMachine.ExpressionBuffer.EndsWith("=")
                ? StateMachine.ExpressionBuffer[..^1].Trim()
                : $"{beforeBuffer} {beforeDisplay}".Trim();

            History.Add(recordedExpr, result.DisplayValue);
        }

        return result;
    }
}
