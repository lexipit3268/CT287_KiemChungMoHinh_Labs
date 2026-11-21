namespace Calculator.Core.Enums;

public enum CalculatorState
{
    Start,              // Initial state, display is "0"
    InputOperand,       // User is entering digits / decimal point
    OperatorEntered,    // An operator (+, -, *, /, ^, %) was just entered
    OpenParenEntered,   // '(' was just entered
    CloseParenEntered,  // ')' was just entered
    FunctionEntered,    // A scientific function (sin, cos, sqrt...) was entered
    Evaluated,          // Result evaluated after '='
    Error               // Error state (DivideByZero, DomainError, etc.)
}

public enum CalculatorMode
{
    Standard,
    Scientific
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
    Backspace
}
