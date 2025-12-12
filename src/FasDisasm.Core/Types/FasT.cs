namespace FasDisasm.Core.Types;

/// <summary>
/// Represents the T (true) type in Visual Lisp (opcode 0x02).
/// T is the boolean true value.
/// </summary>
public sealed class FasT : IFasType
{
    public const byte OpcodeValue = 0x02;

    /// <summary>
    /// Singleton instance of FasT.
    /// </summary>
    public static readonly FasT Instance = new();

    public string TypeName => "T";
    public byte Opcode => OpcodeValue;

    private FasT() { }

    public string ToLispString() => "T";

    public override string ToString() => ToLispString();
}
