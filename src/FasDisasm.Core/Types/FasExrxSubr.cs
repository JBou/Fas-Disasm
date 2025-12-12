namespace FasDisasm.Core.Types;

/// <summary>
/// Represents an external/registered subroutine (EXRXSUBR) in Visual Lisp.
/// These are functions registered from ObjectARX or .NET applications.
/// </summary>
public sealed class FasExrxSubr : IFasType<string>
{
    public string TypeName => "EXRXSUBR";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the external function name.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public FasExrxSubr() { }

    public FasExrxSubr(string value)
    {
        Value = value;
    }

    public string ToLispString() => $"#<EXRXSUBR {Value}>";

    public override string ToString() => Value;
}
