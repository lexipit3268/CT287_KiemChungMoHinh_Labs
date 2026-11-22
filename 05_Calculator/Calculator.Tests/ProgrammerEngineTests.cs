using Calculator.Core.Enums;
using Calculator.Core.Services;

namespace Calculator.Tests;

[TestClass]
public class ProgrammerEngineTests
{
    [TestMethod]
    public void TestProgrammer_BaseAlphabetGuards()
    {
        var prog = new ProgrammerEngine();

        // 1. In BIN base: only '0' and '1' allowed
        prog.CurrentBase = NumberBase.Bin;
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('0'));
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('1'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('2'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('8'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('A'));

        // 2. In OCT base: '0' to '7' allowed
        prog.CurrentBase = NumberBase.Oct;
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('7'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('8'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('A'));

        // 3. In DEC base: '0' to '9' allowed
        prog.CurrentBase = NumberBase.Dec;
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('9'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('A'));

        // 4. In HEX base: '0' to '9', 'A' to 'F' allowed
        prog.CurrentBase = NumberBase.Hex;
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('9'));
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('A'));
        Assert.IsTrue(prog.IsKeyValidForCurrentBase('f'));
        Assert.IsFalse(prog.IsKeyValidForCurrentBase('G'));
    }

    [TestMethod]
    public void TestProgrammer_SimultaneousBaseRepresentations()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Dec;
        prog.CurrentWordSize = BitWordSize.Qword;

        // Type 255 in DEC
        prog.ProcessKey('2');
        prog.ProcessKey('5');
        prog.ProcessKey('5');

        var rep = prog.GetRepresentation();
        Assert.AreEqual("FF", rep.Hex);
        Assert.AreEqual("255", rep.Dec);
        Assert.AreEqual("377", rep.Oct);
        Assert.AreEqual("11111111", rep.Bin);
        Assert.IsTrue(rep.FormattedBin.EndsWith("1111 1111"));
    }

    [TestMethod]
    public void TestProgrammer_BitwiseOperations()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Hex;
        prog.CurrentWordSize = BitWordSize.Byte; // 8-bit

        // 0x0F AND 0x03 = 0x03
        prog.ProcessKey('0');
        prog.ProcessKey('F');
        prog.ProcessOperator("AND");
        prog.ProcessKey('0');
        prog.ProcessKey('3');
        prog.Evaluate();
        Assert.AreEqual(0x03UL, prog.CurrentValue);

        // 0x03 OR 0x40 = 0x43
        prog.ProcessOperator("OR");
        prog.ProcessKey('4');
        prog.ProcessKey('0');
        prog.Evaluate();
        Assert.AreEqual(0x43UL, prog.CurrentValue);

        // 0x43 XOR 0x43 = 0x00
        prog.ProcessOperator("XOR");
        prog.ProcessKey('4');
        prog.ProcessKey('3');
        prog.Evaluate();
        Assert.AreEqual(0x00UL, prog.CurrentValue);
    }

    [TestMethod]
    public void TestProgrammer_WordSizeMasking()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Hex;
        prog.CurrentWordSize = BitWordSize.Qword;

        // Enter 0x123456789ABCDEF0
        prog.CurrentValue = 0x123456789ABCDEF0UL;

        // Switch to Dword (32-bit) -> masks to 0x9ABCDEF0
        prog.CurrentWordSize = BitWordSize.Dword;
        Assert.AreEqual(0x9ABCDEF0UL, prog.CurrentValue);

        // Switch to Word (16-bit) -> masks to 0xDEF0
        prog.CurrentWordSize = BitWordSize.Word;
        Assert.AreEqual(0xDEF0UL, prog.CurrentValue);

        // Switch to Byte (8-bit) -> masks to 0xF0
        prog.CurrentWordSize = BitWordSize.Byte;
        Assert.AreEqual(0xF0UL, prog.CurrentValue);
    }

    [TestMethod]
    public void TestProgrammer_BitShiftAndBitToggle()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Hex;
        prog.CurrentWordSize = BitWordSize.Byte;

        // 0x01 << 3 = 0x08
        prog.ProcessKey('1');
        prog.ProcessOperator("LSH");
        prog.ProcessKey('3');
        prog.Evaluate();
        Assert.AreEqual(0x08UL, prog.CurrentValue);

        // Toggle bit 0 -> 0x08 ^ 0x01 = 0x09
        prog.ToggleBit(0);
        Assert.AreEqual(0x09UL, prog.CurrentValue);
        Assert.IsTrue(prog.IsBitSet(0));
        Assert.IsTrue(prog.IsBitSet(3));
        Assert.IsFalse(prog.IsBitSet(1));
    }

    [TestMethod]
    public void TestProgrammer_DivisionByZero_SetsErrorFlag()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Dec;
        prog.ProcessKey('1');
        prog.ProcessKey('0');
        prog.ProcessOperator("/");
        prog.ProcessKey('0');
        prog.Evaluate();

        Assert.IsTrue(prog.HasError);
        Assert.AreEqual("Cannot divide by zero", prog.ErrorMessage);
        Assert.AreEqual("Cannot divide by zero", prog.FormatForCurrentBase(prog.CurrentValue));

        // Further key inputs blocked when in error state
        bool keyAccepted = prog.ProcessKey('5');
        Assert.IsFalse(keyAccepted);

        // Clear resets error state
        prog.Clear();
        Assert.IsFalse(prog.HasError);
        Assert.AreEqual("0", prog.FormatForCurrentBase(prog.CurrentValue));
    }

    [TestMethod]
    public void TestProgrammer_ModuloByZero_SetsErrorFlag()
    {
        var prog = new ProgrammerEngine();
        prog.CurrentBase = NumberBase.Dec;
        prog.ProcessKey('8');
        prog.ProcessOperator("MOD");
        prog.ProcessKey('0');
        prog.Evaluate();

        Assert.IsTrue(prog.HasError);
        Assert.AreEqual("Cannot divide by zero", prog.ErrorMessage);

        // ClearEntry resets error state
        prog.ClearEntry();
        Assert.IsFalse(prog.HasError);
    }
}
