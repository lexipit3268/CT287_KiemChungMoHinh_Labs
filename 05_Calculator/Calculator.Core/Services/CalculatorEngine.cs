using Calculator.Core.Enums;
using Calculator.Core.Models;
using Calculator.Core.StateMachine;

namespace Calculator.Core.Services;

public class CalculatorEngine
{
    public CalculatorStateMachine StateMachine { get; }
    public IMemoryService Memory { get; }
    public IHistoryService History { get; }
    public ProgrammerEngine Programmer { get; }
    public ModelCheckingService ModelChecker { get; }
    public AstVisualizerService AstVisualizer { get; }
    public ExpressionTreeBuilder TreeBuilder { get; }

    public CalculatorMode Mode { get; set; } = CalculatorMode.Standard;

    public AngleUnit AngleMode
    {
        get => StateMachine.AngleMode;
        set => StateMachine.AngleMode = value;
    }

    public CalculatorEngine(
        CalculatorStateMachine? stateMachine = null,
        IMemoryService? memory = null,
        IHistoryService? history = null,
        ProgrammerEngine? programmer = null,
        ModelCheckingService? modelChecker = null,
        AstVisualizerService? astVisualizer = null,
        ExpressionTreeBuilder? treeBuilder = null)
    {
        StateMachine = stateMachine ?? new CalculatorStateMachine();
        Memory = memory ?? new MemoryService();
        History = history ?? new HistoryService();
        Programmer = programmer ?? new ProgrammerEngine();
        ModelChecker = modelChecker ?? new ModelCheckingService();
        AstVisualizer = astVisualizer ?? new AstVisualizerService();
        TreeBuilder = treeBuilder ?? new ExpressionTreeBuilder();
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
