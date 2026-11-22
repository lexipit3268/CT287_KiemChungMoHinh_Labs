namespace Calculator.Core.Enums;

public enum CalculatorState
{
    Start,              // Initial state, display is "0"
    InputOperand,       // User is entering digits / decimal point
    OperatorEntered,    // An operator (+, -, *, /, ^, %, AND, OR, etc.) was just entered
    OpenParenEntered,   // '(' was just entered
    CloseParenEntered,  // ')' was just entered
    FunctionEntered,    // A scientific function (sin, cos, sqrt...) was entered
    Evaluated,          // Result evaluated after '='
    Error               // Error state (DivideByZero, DomainError, etc.)
}

public enum CalculatorMode
{
    Standard,
    Scientific,
    Programmer
}

public enum NumberBase
{
    Hex = 16,
    Dec = 10,
    Oct = 8,
    Bin = 2
}

public enum BitWordSize
{
    Qword = 64, // 64-bit
    Dword = 32, // 32-bit
    Word = 16,  // 16-bit
    Byte = 8    // 8-bit
}

public enum AngleUnit
{
    Degree,
    Radian
}

public enum CalculatorInputType
{
    Digit,
    DecimalPoint,
    BinaryOperator,
    UnaryFunction,
    Constant,
    OpenParen,
    CloseParen,
    ToggleSign,
    Equals,
    Clear,
    ClearEntry,
    Backspace,
    BitwiseOperator,
    BaseSwitch,
    WordSizeSwitch
}
