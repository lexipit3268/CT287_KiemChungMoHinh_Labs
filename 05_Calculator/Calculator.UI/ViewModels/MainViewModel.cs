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
    private bool _isScientificMode = false;
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

    // Properties
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
            return 23;
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

    public bool IsScientificMode
    {
        get => _isScientificMode;
        set
        {
            if (SetProperty(ref _isScientificMode, value))
            {
                ModeTitle = value ? "Scientific" : "Standard";
                OnPropertyChanged(nameof(IsStandardMode));
            }
        }
    }

    public bool IsStandardMode => !IsScientificMode;

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

    // Parameterless constructor for WPF XAML loader
    public MainViewModel() : this(new CalculatorEngine())
    {
    }

    public MainViewModel(CalculatorEngine engine)
    {
        _engine = engine ?? new CalculatorEngine();

        DigitCommand = new RelayCommand(param =>
        {
            if (param is string s && s.Length > 0)
            {
                _engine.StateMachine.ProcessDigit(s[0]);
                SyncFromState();
            }
        });

        DecimalPointCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.ProcessDecimalPoint();
            SyncFromState();
        });

        OperatorCommand = new RelayCommand(param =>
        {
            if (param is string op)
            {
                _engine.StateMachine.ProcessBinaryOperator(op);
                SyncFromState();
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
            _engine.StateMachine.ProcessOpenParen();
            SyncFromState();
        });

        CloseParenCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.ProcessCloseParen();
            SyncFromState();
        });

        ToggleSignCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.ToggleSign();
            SyncFromState();
        });

        EqualsCommand = new RelayCommand(() =>
        {
            var res = _engine.EvaluateAndRecord();
            SyncFromState();
            SyncHistory();
        });

        ClearCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.Reset();
            SyncFromState();
        });

        ClearEntryCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.ClearEntry();
            SyncFromState();
        });

        BackspaceCommand = new RelayCommand(() =>
        {
            _engine.StateMachine.Backspace();
            SyncFromState();
        });

        // Toggle 2nd Functions
        Toggle2ndCommand = new RelayCommand(() =>
        {
            Is2ndActive = !Is2ndActive;
        });

        // Toggle F-E
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

        // Toggle Angle DEG / RAD
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

        // Navigation Drawer
        ToggleNavDrawerCommand = new RelayCommand(() =>
        {
            IsNavDrawerOpen = !IsNavDrawerOpen;
        });

        SelectModeCommand = new RelayCommand(param =>
        {
            if (param is string modeStr)
            {
                if (modeStr == "Standard")
                {
                    IsScientificMode = false;
                    _engine.Mode = CalculatorMode.Standard;
                }
                else if (modeStr == "Scientific")
                {
                    IsScientificMode = true;
                    _engine.Mode = CalculatorMode.Scientific;
                }
                IsNavDrawerOpen = false;
            }
        });

        // Panels and Flyouts
        ToggleHistoryCommand = new RelayCommand(() =>
        {
            IsHistoryOpen = !IsHistoryOpen;
            if (IsHistoryOpen) IsMemoryDrawerOpen = false;
        });

        ToggleMemoryDrawerCommand = new RelayCommand(() =>
        {
            IsMemoryDrawerOpen = !IsMemoryDrawerOpen;
            if (IsMemoryDrawerOpen) IsHistoryOpen = false;
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
                _engine.StateMachine.LoadCalculation(item.Expression, item.Result);
                SyncFromState();
                IsHistoryOpen = false;
            }
        });

        ClearHistoryCommand = new RelayCommand(() =>
        {
            _engine.History.Clear();
            SyncHistory();
        });

        SyncFromState();
        SyncMemory();
    }

    public void HandleKeyboardInput(Key key, ModifierKeys modifiers)
    {
        bool shift = (modifiers & ModifierKeys.Shift) != 0;

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
