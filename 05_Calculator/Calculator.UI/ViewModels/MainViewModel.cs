using System.Collections.ObjectModel;
using System.Windows.Input;
using Calculator.Core.Enums;
using Calculator.Core.Models;
using Calculator.Core.Services;
using Calculator.UI.Common;

namespace Calculator.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CalculatorEngine _engine;

    private string _display = "0";
    private string _expressionDisplay = string.Empty;
    private string _stateName = "● S0: READY";
    private string _modeTitle = "Standard";
    private string _openParenBadge = string.Empty;
    private CalculatorMode _currentMode = CalculatorMode.Standard;

    private bool _isNavDrawerOpen = false;
    private bool _isHistoryOpen = false;
    private bool _isMemoryDrawerOpen = false;
    private bool _isTrigFlyoutOpen = false;
    private bool _isFuncFlyoutOpen = false;
    private bool _is2ndActive = false;
    private bool _isFEActive = false;
    private string _angleUnitText = "DEG";
    private string _memoryText = string.Empty;
    private bool _hasMemory = false;

    // Expression Parse Tree & AST Visualizer Modal (Session 5 - Expression)
    private bool _isTreeModalOpen = false;
    private bool _isParseTreeMode = false; // false = Compact AST, true = Full Parse Tree (E -> T -> F)
    private double _treeCanvasWidth = 600;
    private double _treeCanvasHeight = 400;
    private string _treeExpressionText = string.Empty;
    private string _treeResultText = string.Empty;
    private string _treeRpnText = string.Empty;

    // Programmer Mode values
    private ProgrammerValueRepresentation _programmerValues = new();

    // Mode properties
    public CalculatorMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (SetProperty(ref _currentMode, value))
            {
                _engine.Mode = value;
                ModeTitle = value switch
                {
                    CalculatorMode.Standard => "Standard",
                    CalculatorMode.Scientific => "Scientific",
                    CalculatorMode.Programmer => "Programmer",
                    _ => "Calculator"
                };
                OnPropertyChanged(nameof(IsStandardMode));
                OnPropertyChanged(nameof(IsScientificMode));
                OnPropertyChanged(nameof(IsProgrammerMode));

                if (value == CalculatorMode.Programmer)
                {
                    SyncProgrammerDisplay();
                }
                else
                {
                    SyncFromState();
                }
            }
        }
    }

    public bool IsStandardMode => CurrentMode == CalculatorMode.Standard;
    public bool IsScientificMode => CurrentMode == CalculatorMode.Scientific;
    public bool IsProgrammerMode => CurrentMode == CalculatorMode.Programmer;

    // Display Properties
    public string Display
    {
        get => _display;
        set
        {
            if (SetProperty(ref _display, value))
            {
                OnPropertyChanged(nameof(DisplayFontSize));
            }
        }
    }

    public double DisplayFontSize
    {
        get
        {
            int len = Display.Length;
            if (len <= 10) return 46;
            if (len == 11) return 42;
            if (len == 12) return 38;
            if (len == 13) return 35;
            if (len == 14) return 32;
            if (len == 15) return 29;
            if (len == 16) return 27;
            if (len == 17) return 25;
            if (len == 18) return 23;
            if (len == 19) return 21;
            if (len <= 21) return 19;
            if (len <= 24) return 17;
            if (len <= 28) return 15;
            if (len <= 34) return 13.5;
            return 12;
        }
    }

    public string ExpressionDisplay
    {
        get => _expressionDisplay;
        set => SetProperty(ref _expressionDisplay, value);
    }

    public string StateName
    {
        get => _stateName;
        set => SetProperty(ref _stateName, value);
    }

    public string ModeTitle
    {
        get => _modeTitle;
        set => SetProperty(ref _modeTitle, value);
    }

    public string OpenParenBadge
    {
        get => _openParenBadge;
        set => SetProperty(ref _openParenBadge, value);
    }

    public bool IsNavDrawerOpen
    {
        get => _isNavDrawerOpen;
        set => SetProperty(ref _isNavDrawerOpen, value);
    }

    public bool IsHistoryOpen
    {
        get => _isHistoryOpen;
        set => SetProperty(ref _isHistoryOpen, value);
    }

    public bool IsMemoryDrawerOpen
    {
        get => _isMemoryDrawerOpen;
        set => SetProperty(ref _isMemoryDrawerOpen, value);
    }

    public bool IsTrigFlyoutOpen
    {
        get => _isTrigFlyoutOpen;
        set => SetProperty(ref _isTrigFlyoutOpen, value);
    }

    public bool IsFuncFlyoutOpen
    {
        get => _isFuncFlyoutOpen;
        set => SetProperty(ref _isFuncFlyoutOpen, value);
    }

    public bool Is2ndActive
    {
        get => _is2ndActive;
        set => SetProperty(ref _is2ndActive, value);
    }

    public bool IsFEActive
    {
        get => _isFEActive;
        set => SetProperty(ref _isFEActive, value);
    }

    public string AngleUnitText
    {
        get => _angleUnitText;
        set => SetProperty(ref _angleUnitText, value);
    }

    public string MemoryText
    {
        get => _memoryText;
        set => SetProperty(ref _memoryText, value);
    }

    public bool HasMemory
    {
        get => _hasMemory;
        set => SetProperty(ref _hasMemory, value);
    }

    // Programmer Mode Properties
    public ProgrammerValueRepresentation ProgrammerValues
    {
        get => _programmerValues;
        private set => SetProperty(ref _programmerValues, value);
    }

    public NumberBase SelectedBase => _engine.Programmer.CurrentBase;
    public BitWordSize SelectedWordSize => _engine.Programmer.CurrentWordSize;

    public bool IsHexBase => SelectedBase == NumberBase.Hex;
    public bool IsDecBase => SelectedBase == NumberBase.Dec;
    public bool IsOctBase => SelectedBase == NumberBase.Oct;
    public bool IsBinBase => SelectedBase == NumberBase.Bin;

    public bool CanInputHexDigits => SelectedBase == NumberBase.Hex;
    public bool CanInputDecimalDigits => SelectedBase == NumberBase.Hex || SelectedBase == NumberBase.Dec;
    public bool CanInputOctalDigits => SelectedBase != NumberBase.Bin;
    public bool CanInputBinaryDigits => true;

    public string HexDisplay => ProgrammerValues.Hex;
    public string DecDisplay => ProgrammerValues.Dec;
    public string OctDisplay => ProgrammerValues.Oct;
    public string BinDisplay => ProgrammerValues.FormattedBin;
    public string WordSizeText => SelectedWordSize.ToString().ToUpperInvariant();

    // Expression Tree Modal Properties
    public bool IsTreeModalOpen
    {
        get => _isTreeModalOpen;
        set => SetProperty(ref _isTreeModalOpen, value);
    }

    public bool IsParseTreeMode
    {
        get => _isParseTreeMode;
        set
        {
            if (SetProperty(ref _isParseTreeMode, value))
            {
                OnPropertyChanged(nameof(IsAstMode));
                RefreshTreeGraph();
            }
        }
    }

    public bool IsAstMode => !IsParseTreeMode;

    public double TreeCanvasWidth
    {
        get => _treeCanvasWidth;
        set => SetProperty(ref _treeCanvasWidth, value);
    }

    public double TreeCanvasHeight
    {
        get => _treeCanvasHeight;
        set => SetProperty(ref _treeCanvasHeight, value);
    }

    public string TreeExpressionText
    {
        get => _treeExpressionText;
        set => SetProperty(ref _treeExpressionText, value);
    }

    public string TreeResultText
    {
        get => _treeResultText;
        set => SetProperty(ref _treeResultText, value);
    }

    public string TreeRpnText
    {
        get => _treeRpnText;
        set => SetProperty(ref _treeRpnText, value);
    }

    public ObservableCollection<ExpressionTreeNode> TreeNodes { get; } = new();
    public ObservableCollection<ExpressionTreeEdge> TreeEdges { get; } = new();
    public ObservableCollection<HistoryItem> History { get; } = new();

    // Commands
    public ICommand DigitCommand { get; }
    public ICommand DecimalPointCommand { get; }
    public ICommand OperatorCommand { get; }
    public ICommand FunctionCommand { get; }
    public ICommand ImmediateFunctionCommand { get; }
    public ICommand ConstantCommand { get; }
    public ICommand OpenParenCommand { get; }
    public ICommand CloseParenCommand { get; }
    public ICommand ToggleSignCommand { get; }
    public ICommand EqualsCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ClearEntryCommand { get; }
    public ICommand BackspaceCommand { get; }

    public ICommand Toggle2ndCommand { get; }
    public ICommand ToggleFECommand { get; }
    public ICommand ToggleAngleCommand { get; }
    public ICommand ToggleNavDrawerCommand { get; }
    public ICommand SelectModeCommand { get; }
    public ICommand ToggleHistoryCommand { get; }
    public ICommand ToggleMemoryDrawerCommand { get; }
    public ICommand ToggleTrigFlyoutCommand { get; }
    public ICommand ToggleFuncFlyoutCommand { get; }

    public ICommand MemoryClearCommand { get; }
    public ICommand MemoryRecallCommand { get; }
    public ICommand MemoryAddCommand { get; }
    public ICommand MemorySubtractCommand { get; }
    public ICommand MemoryStoreCommand { get; }

    public ICommand RecallHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    // Programmer Commands
    public ICommand SelectBaseCommand { get; }
    public ICommand SelectWordSizeCommand { get; }
    public ICommand ProgrammerKeyCommand { get; }
    public ICommand ProgrammerOperatorCommand { get; }
    public ICommand ProgrammerUnaryCommand { get; }
    public ICommand ToggleBitCommand { get; }

    // Tree Visualizer Modal Commands
    public ICommand ToggleTreeModalCommand { get; }
    public ICommand CloseTreeModalCommand { get; }
    public ICommand SwitchToAstModeCommand { get; }
    public ICommand SwitchToParseTreeModeCommand { get; }
    public ICommand RefreshTreeCommand { get; }

    // Parameterless constructor for WPF XAML
    public MainViewModel() : this(new CalculatorEngine())
    {
    }

    public MainViewModel(CalculatorEngine engine)
    {
        _engine = engine ?? new CalculatorEngine();

        // 1. Calculator Basic Commands
        DigitCommand = new RelayCommand(param =>
        {
            if (param is string s && s.Length > 0)
            {
                if (IsProgrammerMode)
                {
                    _engine.Programmer.ProcessKey(s[0]);
                    SyncProgrammerDisplay();
                }
                else
                {
                    _engine.StateMachine.ProcessDigit(s[0]);
                    SyncFromState();
                }
            }
        });

        DecimalPointCommand = new RelayCommand(() =>
        {
            if (!IsProgrammerMode)
            {
                _engine.StateMachine.ProcessDecimalPoint();
                SyncFromState();
            }
        });

        OperatorCommand = new RelayCommand(param =>
        {
            if (param is string op)
            {
                if (IsProgrammerMode)
                {
                    _engine.Programmer.ProcessOperator(op);
                    SyncProgrammerDisplay();
                }
                else
                {
                    _engine.StateMachine.ProcessBinaryOperator(op);
                    SyncFromState();
                }
            }
        });

        FunctionCommand = new RelayCommand(param =>
        {
            if (param is string func)
            {
                _engine.StateMachine.ProcessFunction(func);
                IsTrigFlyoutOpen = false;
                IsFuncFlyoutOpen = false;
                SyncFromState();
            }
        });

        ImmediateFunctionCommand = new RelayCommand(param =>
        {
            if (param is string func)
            {
                _engine.StateMachine.ProcessImmediateFunction(func);
                IsTrigFlyoutOpen = false;
                IsFuncFlyoutOpen = false;
                SyncFromState();
            }
        });

        ConstantCommand = new RelayCommand(param =>
        {
            if (param is string constant)
            {
                _engine.StateMachine.ProcessConstant(constant);
                SyncFromState();
            }
        });

        OpenParenCommand = new RelayCommand(() =>
        {
            if (!IsProgrammerMode)
            {
                _engine.StateMachine.ProcessOpenParen();
                SyncFromState();
            }
        });

        CloseParenCommand = new RelayCommand(() =>
        {
            if (!IsProgrammerMode)
            {
                _engine.StateMachine.ProcessCloseParen();
                SyncFromState();
            }
        });

        ToggleSignCommand = new RelayCommand(() =>
        {
            if (IsProgrammerMode)
            {
                _engine.Programmer.ProcessUnary("NEG");
                SyncProgrammerDisplay();
            }
            else
            {
                _engine.StateMachine.ToggleSign();
                SyncFromState();
            }
        });

        EqualsCommand = new RelayCommand(() =>
        {
            if (IsProgrammerMode)
            {
                _engine.Programmer.Evaluate();
                SyncProgrammerDisplay();
            }
            else
            {
                var res = _engine.EvaluateAndRecord();
                SyncFromState();
                SyncHistory();
            }
        });

        ClearCommand = new RelayCommand(() =>
        {
            if (IsProgrammerMode)
            {
                _engine.Programmer.Clear();
                SyncProgrammerDisplay();
            }
            else
            {
                _engine.StateMachine.Reset();
                SyncFromState();
            }
        });

        ClearEntryCommand = new RelayCommand(() =>
        {
            if (IsProgrammerMode)
            {
                _engine.Programmer.ClearEntry();
                SyncProgrammerDisplay();
            }
            else
            {
                _engine.StateMachine.ClearEntry();
                SyncFromState();
            }
        });

        BackspaceCommand = new RelayCommand(() =>
        {
            if (IsProgrammerMode)
            {
                _engine.Programmer.Backspace();
                SyncProgrammerDisplay();
            }
            else
            {
                _engine.StateMachine.Backspace();
                SyncFromState();
            }
        });

        // 2. Toggles
        Toggle2ndCommand = new RelayCommand(() =>
        {
            Is2ndActive = !Is2ndActive;
        });

        ToggleFECommand = new RelayCommand(() =>
        {
            IsFEActive = !IsFEActive;
            if (double.TryParse(Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                Display = IsFEActive
                    ? val.ToString("E4", System.Globalization.CultureInfo.InvariantCulture)
                    : CalculationResult.FormatNumber(val);
            }
        });

        ToggleAngleCommand = new RelayCommand(() =>
        {
            if (_engine.AngleMode == AngleUnit.Degree)
            {
                _engine.AngleMode = AngleUnit.Radian;
                AngleUnitText = "RAD";
            }
            else
            {
                _engine.AngleMode = AngleUnit.Degree;
                AngleUnitText = "DEG";
            }
        });

        ToggleNavDrawerCommand = new RelayCommand(() =>
        {
            IsNavDrawerOpen = !IsNavDrawerOpen;
        });

        SelectModeCommand = new RelayCommand(param =>
        {
            if (param is string modeStr)
            {
                if (modeStr == "Standard") CurrentMode = CalculatorMode.Standard;
                else if (modeStr == "Scientific") CurrentMode = CalculatorMode.Scientific;
                else if (modeStr == "Programmer") CurrentMode = CalculatorMode.Programmer;
                IsNavDrawerOpen = false;
            }
        });

        ToggleHistoryCommand = new RelayCommand(() =>
        {
            IsHistoryOpen = !IsHistoryOpen;
            if (IsHistoryOpen)
            {
                IsMemoryDrawerOpen = false;
                IsTreeModalOpen = false;
            }
        });

        ToggleMemoryDrawerCommand = new RelayCommand(() =>
        {
            IsMemoryDrawerOpen = !IsMemoryDrawerOpen;
            if (IsMemoryDrawerOpen)
            {
                IsHistoryOpen = false;
                IsTreeModalOpen = false;
            }
        });

        ToggleTrigFlyoutCommand = new RelayCommand(() =>
        {
            IsTrigFlyoutOpen = !IsTrigFlyoutOpen;
            if (IsTrigFlyoutOpen) IsFuncFlyoutOpen = false;
        });

        ToggleFuncFlyoutCommand = new RelayCommand(() =>
        {
            IsFuncFlyoutOpen = !IsFuncFlyoutOpen;
            if (IsFuncFlyoutOpen) IsTrigFlyoutOpen = false;
        });

        // Memory Commands
        MemoryClearCommand = new RelayCommand(() =>
        {
            _engine.Memory.Clear();
            SyncMemory();
        });

        MemoryRecallCommand = new RelayCommand(() =>
        {
            if (_engine.Memory.HasMemory)
            {
                string memStr = CalculationResult.FormatNumber(_engine.Memory.Recall());
                if (_engine.StateMachine.CurrentState == CalculatorState.Start || _engine.StateMachine.CurrentState == CalculatorState.Evaluated)
                {
                    _engine.StateMachine.Reset();
                }
                foreach (char c in memStr)
                {
                    if (c == '.') _engine.StateMachine.ProcessDecimalPoint();
                    else if (char.IsDigit(c)) _engine.StateMachine.ProcessDigit(c);
                    else if (c == '-') _engine.StateMachine.ToggleSign();
                }
                SyncFromState();
            }
        });

        MemoryAddCommand = new RelayCommand(() =>
        {
            if (double.TryParse(_engine.StateMachine.Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                _engine.Memory.Add(val);
                SyncMemory();
            }
        });

        MemorySubtractCommand = new RelayCommand(() =>
        {
            if (double.TryParse(_engine.StateMachine.Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                _engine.Memory.Subtract(val);
                SyncMemory();
            }
        });

        MemoryStoreCommand = new RelayCommand(() =>
        {
            if (double.TryParse(_engine.StateMachine.Display, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                _engine.Memory.Store(val);
                SyncMemory();
            }
        });

        RecallHistoryCommand = new RelayCommand(param =>
        {
            if (param is HistoryItem item)
            {
                if (IsProgrammerMode)
                {
                    // In programmer mode, parse integer value safely
                    if (double.TryParse(item.Result, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                    {
                        ulong uVal = (ulong)(long)dVal;
                        _engine.Programmer.CurrentValue = uVal;
                        _engine.Programmer.SetExpressionDisplay($"{item.Expression} =");
                        SyncProgrammerDisplay();
                    }
                }
                else
                {
                    _engine.StateMachine.LoadCalculation(item.Expression, item.Result);
                    SyncFromState();
                }
                IsHistoryOpen = false;
            }
        });

        ClearHistoryCommand = new RelayCommand(() =>
        {
            _engine.History.Clear();
            SyncHistory();
        });

        // 3. Programmer Mode Commands
        SelectBaseCommand = new RelayCommand(param =>
        {
            if (param is string baseStr)
            {
                _engine.Programmer.CurrentBase = baseStr.ToUpperInvariant() switch
                {
                    "HEX" => NumberBase.Hex,
                    "DEC" => NumberBase.Dec,
                    "OCT" => NumberBase.Oct,
                    "BIN" => NumberBase.Bin,
                    _ => NumberBase.Dec
                };
                NotifyBaseChanged();
                SyncProgrammerDisplay();
            }
        });

        SelectWordSizeCommand = new RelayCommand(param =>
        {
            if (param is string sizeStr)
            {
                _engine.Programmer.CurrentWordSize = sizeStr.ToUpperInvariant() switch
                {
                    "BYTE" => BitWordSize.Byte,
                    "WORD" => BitWordSize.Word,
                    "DWORD" => BitWordSize.Dword,
                    _ => BitWordSize.Qword
                };
                OnPropertyChanged(nameof(SelectedWordSize));
                OnPropertyChanged(nameof(WordSizeText));
                SyncProgrammerDisplay();
            }
        });

        ProgrammerKeyCommand = new RelayCommand(param =>
        {
            if (param is string s && s.Length > 0)
            {
                _engine.Programmer.ProcessKey(s[0]);
                SyncProgrammerDisplay();
            }
        });

        ProgrammerOperatorCommand = new RelayCommand(param =>
        {
            if (param is string op)
            {
                _engine.Programmer.ProcessOperator(op);
                SyncProgrammerDisplay();
            }
        });

        ProgrammerUnaryCommand = new RelayCommand(param =>
        {
            if (param is string op)
            {
                _engine.Programmer.ProcessUnary(op);
                SyncProgrammerDisplay();
            }
        });

        ToggleBitCommand = new RelayCommand(param =>
        {
            if (param is int bitIdx)
            {
                _engine.Programmer.ToggleBit(bitIdx);
                SyncProgrammerDisplay();
            }
            else if (param is string str && int.TryParse(str, out int parsed))
            {
                _engine.Programmer.ToggleBit(parsed);
                SyncProgrammerDisplay();
            }
        });

        // 4. Expression Tree Visualizer Modal Commands
        ToggleTreeModalCommand = new RelayCommand(() =>
        {
            IsTreeModalOpen = !IsTreeModalOpen;
            if (IsTreeModalOpen)
            {
                IsHistoryOpen = false;
                IsMemoryDrawerOpen = false;
                RefreshTreeGraph();
            }
        });

        CloseTreeModalCommand = new RelayCommand(() =>
        {
            IsTreeModalOpen = false;
        });

        SwitchToAstModeCommand = new RelayCommand(() =>
        {
            IsParseTreeMode = false;
        });

        SwitchToParseTreeModeCommand = new RelayCommand(() =>
        {
            IsParseTreeMode = true;
        });

        RefreshTreeCommand = new RelayCommand(() =>
        {
            RefreshTreeGraph();
        });

        // Initialize state
        SyncFromState();
        SyncMemory();
        SyncProgrammerDisplay();
    }

    public void RefreshTreeGraph()
    {
        string expr = !string.IsNullOrWhiteSpace(ExpressionDisplay)
            ? ExpressionDisplay
            : Display;

        // If ends with '=', remove for parsing
        if (expr.EndsWith('='))
        {
            expr = expr[..^1].Trim();
        }

        var graph = _engine.TreeBuilder.BuildGraph(expr, IsParseTreeMode);

        TreeNodes.Clear();
        foreach (var node in graph.Nodes)
        {
            TreeNodes.Add(node);
        }

        TreeEdges.Clear();
        foreach (var edge in graph.Edges)
        {
            TreeEdges.Add(edge);
        }

        TreeCanvasWidth = graph.CanvasWidth;
        TreeCanvasHeight = graph.CanvasHeight;
        TreeExpressionText = graph.Expression;
        TreeResultText = graph.Result;
        TreeRpnText = graph.Rpn;
    }

    public void HandleKeyboardInput(Key key, ModifierKeys modifiers)
    {
        bool shift = (modifiers & ModifierKeys.Shift) != 0;

        if (IsTreeModalOpen && key == Key.Escape)
        {
            IsTreeModalOpen = false;
            return;
        }

        if (IsProgrammerMode && CanInputHexDigits)
        {
            if (key >= Key.A && key <= Key.F)
            {
                ProgrammerKeyCommand.Execute(key.ToString());
                return;
            }
        }

        switch (key)
        {
            case Key.D0 when !shift:
            case Key.NumPad0:
                DigitCommand.Execute("0");
                break;
            case Key.D1 when !shift:
            case Key.NumPad1:
                DigitCommand.Execute("1");
                break;
            case Key.D2 when !shift:
            case Key.NumPad2:
                DigitCommand.Execute("2");
                break;
            case Key.D3 when !shift:
            case Key.NumPad3:
                DigitCommand.Execute("3");
                break;
            case Key.D4 when !shift:
            case Key.NumPad4:
                DigitCommand.Execute("4");
                break;
            case Key.D5 when !shift:
            case Key.NumPad5:
                DigitCommand.Execute("5");
                break;
            case Key.D6 when !shift:
            case Key.NumPad6:
                DigitCommand.Execute("6");
                break;
            case Key.D7 when !shift:
            case Key.NumPad7:
                DigitCommand.Execute("7");
                break;
            case Key.D8 when !shift:
            case Key.NumPad8:
                DigitCommand.Execute("8");
                break;
            case Key.D9 when !shift:
            case Key.NumPad9:
                DigitCommand.Execute("9");
                break;

            case Key.OemPeriod:
            case Key.Decimal:
                DecimalPointCommand.Execute(null);
                break;

            case Key.Add:
            case Key.OemPlus when shift:
                OperatorCommand.Execute("+");
                break;

            case Key.Subtract:
            case Key.OemMinus when !shift:
                OperatorCommand.Execute("-");
                break;

            case Key.Multiply:
            case Key.D8 when shift:
                OperatorCommand.Execute("*");
                break;

            case Key.Divide:
            case Key.OemQuestion when !shift:
                OperatorCommand.Execute("/");
                break;

            case Key.D5 when shift:
                OperatorCommand.Execute("%");
                break;

            case Key.D6 when shift:
                OperatorCommand.Execute("^");
                break;

            case Key.D1 when shift:
                ImmediateFunctionCommand.Execute("fact");
                break;

            case Key.D9 when shift:
                OpenParenCommand.Execute(null);
                break;

            case Key.D0 when shift:
                CloseParenCommand.Execute(null);
                break;

            case Key.Enter:
                EqualsCommand.Execute(null);
                break;

            case Key.Back:
                BackspaceCommand.Execute(null);
                break;

            case Key.Escape:
                ClearCommand.Execute(null);
                break;
        }
    }

    private void SyncFromState()
    {
        string rawDisplay = _engine.StateMachine.Display;
        if (IsFEActive && double.TryParse(rawDisplay, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            Display = val.ToString("E4", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            Display = rawDisplay;
        }

        ExpressionDisplay = _engine.StateMachine.ExpressionBuffer;
        StateName = FormatStateLabel(_engine.StateMachine.CurrentState);

        int parenCount = _engine.StateMachine.OpenParenCount;
        OpenParenBadge = parenCount > 0 ? $"({parenCount})" : string.Empty;
    }

    private void SyncProgrammerDisplay()
    {
        ProgrammerValues = _engine.Programmer.GetRepresentation();
        if (_engine.Programmer.HasError)
        {
            Display = _engine.Programmer.ErrorMessage ?? "Cannot divide by zero";
            ExpressionDisplay = _engine.Programmer.ExpressionDisplay;
            StateName = "● PROG: ERROR";
        }
        else
        {
            Display = _engine.Programmer.FormatForCurrentBase(_engine.Programmer.CurrentValue);
            ExpressionDisplay = _engine.Programmer.ExpressionDisplay;
            StateName = $"● PROG: {SelectedBase} ({WordSizeText})";
        }

        OnPropertyChanged(nameof(HexDisplay));
        OnPropertyChanged(nameof(DecDisplay));
        OnPropertyChanged(nameof(OctDisplay));
        OnPropertyChanged(nameof(BinDisplay));
        OnPropertyChanged(nameof(WordSizeText));
    }

    private void NotifyBaseChanged()
    {
        OnPropertyChanged(nameof(SelectedBase));
        OnPropertyChanged(nameof(IsHexBase));
        OnPropertyChanged(nameof(IsDecBase));
        OnPropertyChanged(nameof(IsOctBase));
        OnPropertyChanged(nameof(IsBinBase));
        OnPropertyChanged(nameof(CanInputHexDigits));
        OnPropertyChanged(nameof(CanInputDecimalDigits));
        OnPropertyChanged(nameof(CanInputOctalDigits));
        OnPropertyChanged(nameof(CanInputBinaryDigits));
    }

    private void SyncMemory()
    {
        HasMemory = _engine.Memory.HasMemory;
        if (HasMemory)
        {
            MemoryText = $"M: {CalculationResult.FormatNumber(_engine.Memory.MemoryValue)}";
        }
        else
        {
            MemoryText = string.Empty;
        }
    }

    private void SyncHistory()
    {
        History.Clear();
        foreach (var item in _engine.History.Items)
        {
            History.Add(item);
        }
    }

    private static string FormatStateLabel(CalculatorState state) => state switch
    {
        CalculatorState.Start => "● S0: READY",
        CalculatorState.InputOperand => "● S1: NUM",
        CalculatorState.OperatorEntered => "● S2: OPERATOR",
        CalculatorState.OpenParenEntered => "● S3: OPEN_PAREN",
        CalculatorState.CloseParenEntered => "● S4: CLOSE_PAREN",
        CalculatorState.FunctionEntered => "● S5: FUNCTION",
        CalculatorState.Evaluated => "● S6: EVALUATED",
        CalculatorState.Error => "● S7: ERROR",
        _ => $"● {state}"
    };
}
