namespace Calculator.Core.Services;

public interface IMemoryService
{
    double MemoryValue { get; }
    bool HasMemory { get; }
    void Clear();
    double Recall();
    void Store(double value);
    void Add(double value);
    void Subtract(double value);
}

public class MemoryService : IMemoryService
{
    public double MemoryValue { get; private set; } = 0;
    public bool HasMemory { get; private set; } = false;

    public void Clear()
    {
        MemoryValue = 0;
        HasMemory = false;
    }

    public double Recall()
    {
        return MemoryValue;
    }

    public void Store(double value)
    {
        MemoryValue = value;
        HasMemory = true;
    }

    public void Add(double value)
    {
        MemoryValue += value;
        HasMemory = true;
    }

    public void Subtract(double value)
    {
        MemoryValue -= value;
        HasMemory = true;
    }
}
