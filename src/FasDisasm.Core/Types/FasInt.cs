namespace FasDisasm.Core.Types;

/// <summary>
/// Represents an integer type in Visual Lisp (opcode 0x3B).
/// </summary>
public sealed class FasInt : IFasType<long>
{
    public const byte OpcodeValue = 0x3B;

    public string TypeName => "INT";
    public byte Opcode => OpcodeValue;

    /// <summary>
    /// Gets or sets the integer value.
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// Gets or sets the byte size used to store this integer (1, 2, or 4 bytes).
    /// </summary>
    public int Size { get; set; } = 4;

    /// <summary>
    /// When true, this integer represents the T value.
    /// </summary>
    public bool IsT { get; set; }

    public FasInt() { }

    public FasInt(long value, int size = 4)
    {
        Value = value;
        Size = size;
    }

    public string ToLispString() => IsT ? "T" : Value.ToString();

    public override string ToString() => ToLispString();
}
