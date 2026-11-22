using System.Globalization;
using System.Text;
using Calculator.Core.Enums;
using Calculator.Core.Models;

namespace Calculator.Core.Services;

public class ProgrammerEngine
{
    private NumberBase _currentBase = NumberBase.Dec;
    private BitWordSize _currentWordSize = BitWordSize.Qword;
    private ulong _currentValue = 0;
    private ulong? _previousValue = null;
    private string? _pendingOperator = null;
    private bool _isNewInput = true;
    private readonly List<StateTransitionRecord> _programmerTransitionLog = new();
    private int _stepCounter = 0;

    public NumberBase CurrentBase
    {
        get => _currentBase;
        set
        {
            if (_currentBase != value)
            {
                var from = _currentBase;
                _currentBase = value;
                RecordTransition($"Switched base from {from} to {value}");
            }
        }
    }

    public BitWordSize CurrentWordSize
    {
        get => _currentWordSize;
        set
        {
            if (_currentWordSize != value)
            {
                var from = _currentWordSize;
                _currentWordSize = value;
                _currentValue &= GetMask(value);
                if (_previousValue.HasValue)
                {
                    _previousValue = _previousValue.Value & GetMask(value);
                }
                RecordTransition($"Switched word size from {from} to {value} (Mask: 0x{GetMask(value):X})");
            }
        }
    }

    public ulong CurrentValue
    {
        get => _currentValue;
        set => _currentValue = value & Mask;
    }

    public string? PendingOperator => _pendingOperator;
    public string ExpressionDisplay { get; private set; } = string.Empty;
    public bool HasError { get; private set; } = false;
    public string? ErrorMessage { get; private set; } = null;
    public IReadOnlyList<StateTransitionRecord> TransitionLog => _programmerTransitionLog.AsReadOnly();

    public ulong Mask => GetMask(_currentWordSize);

    public ProgrammerEngine()
    {
        Reset();
    }

    public void Reset()
    {
        _currentValue = 0;
        _previousValue = null;
        _pendingOperator = null;
        _isNewInput = true;
        HasError = false;
        ErrorMessage = null;
        ExpressionDisplay = string.Empty;
        RecordTransition("Programmer engine reset");
    }

    public void SetExpressionDisplay(string expr)
    {
        ExpressionDisplay = expr;
        _isNewInput = true;
        HasError = false;
        ErrorMessage = null;
    }

    public static ulong GetMask(BitWordSize size) => size switch
    {
        BitWordSize.Byte => 0x00000000000000FFUL,
        BitWordSize.Word => 0x000000000000FFFFUL,
        BitWordSize.Dword => 0x00000000FFFFFFFFUL,
        BitWordSize.Qword => 0xFFFFFFFFFFFFFFFFUL,
        _ => 0xFFFFFFFFFFFFFFFFUL
    };

    public bool IsKeyValidForCurrentBase(char key)
    {
        char upper = char.ToUpperInvariant(key);
        return _currentBase switch
        {
            NumberBase.Bin => upper == '0' || upper == '1',
            NumberBase.Oct => upper >= '0' && upper <= '7',
            NumberBase.Dec => upper >= '0' && upper <= '9',
            NumberBase.Hex => (upper >= '0' && upper <= '9') || (upper >= 'A' && upper <= 'F'),
            _ => false
        };
    }

    public bool ProcessKey(char key)
    {
        if (HasError || !IsKeyValidForCurrentBase(key)) return false;

        char upper = char.ToUpperInvariant(key);
        int digitVal = upper >= 'A' && upper <= 'F' ? 10 + (upper - 'A') : (upper - '0');

        if (_isNewInput)
        {
            _currentValue = (ulong)digitVal & Mask;
            _isNewInput = false;
        }
        else
        {
            ulong baseVal = (ulong)_currentBase;
            ulong newVal = (_currentValue * baseVal) + (ulong)digitVal;
            _currentValue = newVal & Mask;
        }

        RecordTransition($"Input '{key}' in base {_currentBase} -> Value: 0x{_currentValue:X}");
        return true;
    }

    public void Backspace()
    {
        if (_isNewInput || _currentValue == 0)
        {
            _currentValue = 0;
            _isNewInput = true;
            return;
        }

        ulong baseVal = (ulong)_currentBase;
        _currentValue = (_currentValue / baseVal) & Mask;
        if (_currentValue == 0) _isNewInput = true;
        RecordTransition($"Backspace -> Value: 0x{_currentValue:X}");
    }

    public void Clear()
    {
        Reset();
    }

    public void ClearEntry()
    {
        _currentValue = 0;
        _isNewInput = true;
        HasError = false;
        ErrorMessage = null;
        RecordTransition("Cleared entry");
    }

    public void ToggleBit(int bitIndex)
    {
        if (HasError || bitIndex < 0 || bitIndex >= (int)_currentWordSize) return;

        ulong bitMask = 1UL << bitIndex;
        _currentValue ^= bitMask;
        _currentValue &= Mask;
        _isNewInput = false;
        RecordTransition($"Toggled bit {bitIndex} -> Value: 0x{_currentValue:X}");
    }

    public bool IsBitSet(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= 64) return false;
        return (_currentValue & (1UL << bitIndex)) != 0;
    }

    public void ProcessOperator(string op)
    {
        if (HasError) return;
        string normOp = op.ToUpperInvariant();

        if (_previousValue.HasValue && !string.IsNullOrEmpty(_pendingOperator) && !_isNewInput)
        {
            EvaluatePending();
        }

        _previousValue = _currentValue;
        _pendingOperator = normOp;
        _isNewInput = true;
        ExpressionDisplay = $"{FormatForCurrentBase(_previousValue.Value)} {normOp} ";
        RecordTransition($"Operator '{normOp}' placed");
    }

    public void ProcessUnary(string op)
    {
        if (HasError) return;
        string normOp = op.ToUpperInvariant();
        ulong before = _currentValue;

        switch (normOp)
        {
            case "NOT":
                _currentValue = (~_currentValue) & Mask;
                break;
            case "NEG":
            case "+/-":
                long signed = (long)_currentValue;
                _currentValue = (ulong)(-signed) & Mask;
                break;
        }

        ExpressionDisplay = $"{normOp}({FormatForCurrentBase(before)}) =";
        _isNewInput = true;
        RecordTransition($"Unary '{normOp}' computed: 0x{before:X} -> 0x{_currentValue:X}");
    }

    public void Evaluate()
    {
        if (HasError) return;
        if (!_previousValue.HasValue || string.IsNullOrEmpty(_pendingOperator)) return;

        ulong a = _previousValue.Value;
        ulong b = _currentValue;
        string op = _pendingOperator;

        ExpressionDisplay = $"{FormatForCurrentBase(a)} {op} {FormatForCurrentBase(b)} =";
        EvaluatePending();
        _pendingOperator = null;
        _previousValue = null;
        _isNewInput = true;
        if (!HasError)
        {
            RecordTransition($"Evaluated: {ExpressionDisplay} {FormatForCurrentBase(_currentValue)}");
        }
    }

    private void EvaluatePending()
    {
        if (!_previousValue.HasValue || string.IsNullOrEmpty(_pendingOperator)) return;

        ulong a = _previousValue.Value;
        ulong b = _currentValue;
        ulong res = 0;

        switch (_pendingOperator)
        {
            case "AND":
                res = a & b;
                break;
            case "OR":
                res = a | b;
                break;
            case "XOR":
                res = a ^ b;
                break;
            case "LSH":
            case "<<":
                res = a << (int)(b & 0x3F);
                break;
            case "RSH":
            case ">>":
                res = a >> (int)(b & 0x3F);
                break;
            case "+":
                res = a + b;
                break;
            case "-":
                res = a - b;
                break;
            case "*":
            case "×":
                res = a * b;
                break;
            case "/":
            case "÷":
                if (b == 0)
                {
                    HasError = true;
                    ErrorMessage = "Cannot divide by zero";
                    _currentValue = 0;
                    RecordTransition("Division by zero error");
                    return;
                }
                res = a / b;
                break;
            case "%":
            case "MOD":
                if (b == 0)
                {
                    HasError = true;
                    ErrorMessage = "Cannot divide by zero";
                    _currentValue = 0;
                    RecordTransition("Modulo by zero error");
                    return;
                }
                res = a % b;
                break;
            default:
                res = b;
                break;
        }

        _currentValue = res & Mask;
    }

    public string FormatForCurrentBase(ulong val)
    {
        if (HasError) return ErrorMessage ?? "Cannot divide by zero";

        ulong masked = val & Mask;
        return _currentBase switch
        {
            NumberBase.Hex => masked.ToString("X"),
            NumberBase.Dec => GetSignedDecString(masked),
            NumberBase.Oct => Convert.ToString((long)masked, 8),
            NumberBase.Bin => Convert.ToString((long)masked, 2),
            _ => masked.ToString()
        };
    }

    public ProgrammerValueRepresentation GetRepresentation()
    {
        if (HasError)
        {
            return new ProgrammerValueRepresentation
            {
                Hex = "Error",
                Dec = "Error",
                Oct = "Error",
                Bin = "Error",
                FormattedBin = "Cannot divide by zero"
            };
        }

        ulong val = _currentValue & Mask;
        string hex = val.ToString("X");
        string dec = GetSignedDecString(val);
        string oct = Convert.ToString((long)val, 8);
        string bin = Convert.ToString((long)val, 2);

        // Format binary with 4-bit nibbles padded to word size
        int totalBits = (int)_currentWordSize;
        string paddedBin = bin.PadLeft(totalBits, '0');
        var sb = new StringBuilder();
        for (int i = 0; i < paddedBin.Length; i++)
        {
            sb.Append(paddedBin[i]);
            if ((i + 1) % 4 == 0 && i < paddedBin.Length - 1)
            {
                sb.Append(' ');
            }
        }

        return new ProgrammerValueRepresentation
        {
            RawValue = val,
            Hex = hex,
            Dec = dec,
            Oct = oct,
            Bin = bin,
            FormattedBin = sb.ToString()
        };
    }

    private string GetSignedDecString(ulong val)
    {
        // For signed representation according to word size
        return _currentWordSize switch
        {
            BitWordSize.Byte => ((sbyte)(byte)val).ToString(CultureInfo.InvariantCulture),
            BitWordSize.Word => ((short)(ushort)val).ToString(CultureInfo.InvariantCulture),
            BitWordSize.Dword => ((int)(uint)val).ToString(CultureInfo.InvariantCulture),
            BitWordSize.Qword => ((long)val).ToString(CultureInfo.InvariantCulture),
            _ => val.ToString(CultureInfo.InvariantCulture)
        };
    }

    private void RecordTransition(string desc)
    {
        _stepCounter++;
        _programmerTransitionLog.Add(new StateTransitionRecord
        {
            StepIndex = _stepCounter,
            FromState = CalculatorState.InputOperand,
            InputType = CalculatorInputType.Digit,
            InputValue = FormatForCurrentBase(_currentValue),
            ToState = CalculatorState.InputOperand,
            ActionDescription = desc,
            DisplaySnapshot = FormatForCurrentBase(_currentValue),
            ExpressionSnapshot = ExpressionDisplay,
            Timestamp = DateTime.Now
        });
    }
}
