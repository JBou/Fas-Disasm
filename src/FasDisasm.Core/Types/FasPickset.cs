namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a selection set (PICKSET) type in Visual Lisp.
/// Selection sets are collections of AutoCAD entities.
/// </summary>
public sealed class FasPickset : IFasType<long>
{
    public string TypeName => "PICKSET";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the selection set handle.
    /// </summary>
    public long Value { get; set; }

    public FasPickset() { }

    public FasPickset(long value)
    {
        Value = value;
    }

    public string ToLispString() => $"<Selection set: {Value}>";

    public override string ToString() => ToLispString();
}
