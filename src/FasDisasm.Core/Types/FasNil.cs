namespace FasDisasm.Core.Types;

/// <summary>
/// Represents the NIL type in Visual Lisp (opcode 0x01).
/// NIL represents both the boolean false value and the empty list.
/// </summary>
public sealed class FasNil : IFasType
{
    public const byte OpcodeValue = 0x01;

    /// <summary>
    /// Singleton instance of FasNil.
    /// </summary>
    public static readonly FasNil Instance = new();

    /// <summary>
    /// When true, the value is suppressed in output (used for implicit returns).
    /// </summary>
    public bool SuppressOutput { get; set; }

    public string TypeName => "NIL";
    public byte Opcode => OpcodeValue;

    private FasNil() { }

    public string ToLispString() => SuppressOutput ? string.Empty : "nil";

    public override string ToString() => ToLispString();
}
