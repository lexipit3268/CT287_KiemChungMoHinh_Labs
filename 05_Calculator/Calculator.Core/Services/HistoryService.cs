using Calculator.Core.Models;

namespace Calculator.Core.Services;

public interface IHistoryService
{
    IReadOnlyList<HistoryItem> Items { get; }
    void Add(string expression, string result);
    void Clear();
}

public class HistoryService : IHistoryService
{
    private readonly List<HistoryItem> _items = new();

    public IReadOnlyList<HistoryItem> Items => _items.AsReadOnly();

    public void Add(string expression, string result)
    {
        _items.Insert(0, new HistoryItem(expression, result));
        // Keep latest 100 entries
        if (_items.Count > 100)
        {
            _items.RemoveAt(_items.Count - 1);
        }
    }

    public void Clear()
    {
        _items.Clear();
    }
}
