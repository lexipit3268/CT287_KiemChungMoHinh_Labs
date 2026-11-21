using Calculator.Core.Services;

namespace Calculator.Tests;

[TestClass]
public class MemoryAndHistoryTests
{
    [TestMethod]
    public void TestMemory_StoreRecallAddSubtractClear()
    {
        var mem = new MemoryService();
        Assert.IsFalse(mem.HasMemory);

        mem.Store(42.5);
        Assert.IsTrue(mem.HasMemory);
        Assert.AreEqual(42.5, mem.Recall());

        mem.Add(10);
        Assert.AreEqual(52.5, mem.Recall());

        mem.Subtract(2.5);
        Assert.AreEqual(50.0, mem.Recall());

        mem.Clear();
        Assert.IsFalse(mem.HasMemory);
        Assert.AreEqual(0.0, mem.Recall());
    }

    [TestMethod]
    public void TestHistory_AddAndClear()
    {
        var history = new HistoryService();
        Assert.AreEqual(0, history.Items.Count);

        history.Add("2 + 3", "5");
        history.Add("10 * 4", "40");

        Assert.AreEqual(2, history.Items.Count);
        Assert.AreEqual("10 * 4", history.Items[0].Expression);
        Assert.AreEqual("40", history.Items[0].Result);

        history.Clear();
        Assert.AreEqual(0, history.Items.Count);
    }
}
