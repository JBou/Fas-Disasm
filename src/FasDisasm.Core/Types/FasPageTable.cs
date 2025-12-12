namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a page table structure in Visual Lisp.
/// Used internally for memory management.
/// </summary>
public sealed class FasPageTable : IFasType<long>
{
    public string TypeName => "PAGETB";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the page table reference.
    /// </summary>
    public long Value { get; set; }

    public FasPageTable() { }

    public FasPageTable(long value)
    {
        Value = value;
    }

    public string ToLispString() => $"#<PAGETB {Value:X8}>";

    public override string ToString() => ToLispString();
}
