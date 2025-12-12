namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a symbol type in Visual Lisp (opcode 0x5B for FAS, 0x56 for FSL).
/// Symbols are identifiers like variable names, function names, etc.
/// </summary>
public sealed class FasSymbol : IFasType<string>
{
    public const byte OpcodeFas = 0x5B;
    public const byte OpcodeFsl = 0x56;

    private static readonly HashSet<string> KnownTypeSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "EXRXSUBR", "LIST", "SUBR", "FILE", "ENAME", "PICKSET", "REAL", "INT", "STR"
    };

    public string TypeName => "SYM";
    public byte Opcode => OpcodeFas;

    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// When true, the symbol is quoted (prefixed with ').
    /// </summary>
    public bool IsQuoted { get; set; }

    public FasSymbol() { }

    public FasSymbol(string value, bool isQuoted = false)
    {
        Value = value;
        IsQuoted = isQuoted;
    }

    public string ToLispString()
    {
        if (IsQuoted && !KnownTypeSymbols.Contains(Value))
        {
            return $"'{Value}";
        }
        return Value;
    }

    public override string ToString() => ToLispString();
}
