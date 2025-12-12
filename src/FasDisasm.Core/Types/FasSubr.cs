namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a built-in subroutine (SUBR) in Visual Lisp.
/// SUBRs are internal AutoLISP functions like +, -, car, cdr, etc.
/// </summary>
public sealed class FasSubr : IFasType<string>
{
    public string TypeName => "SUBR";
    public byte Opcode => 0x00; // SUBRs don't have a specific opcode

    /// <summary>
    /// Gets or sets the subroutine name.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subroutine index/ID.
    /// </summary>
    public int Index { get; set; }

    public FasSubr() { }

    public FasSubr(string name, int index = 0)
    {
        Value = name;
        Index = index;
    }

    public string ToLispString() => $"#<SUBR @{Index:X8} {Value}>";

    public override string ToString() => Value;
}
