namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a local variable reference in Visual Lisp.
/// </summary>
public sealed class FasLocalVar : IFasType<string>
{
    public string TypeName => "LVAR";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the variable name or value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local variable index.
    /// </summary>
    public int Index { get; set; }

    public FasLocalVar() { }

    public FasLocalVar(int index, string? name = null)
    {
        Index = index;
        Value = name ?? $"_fas{index}";
    }

    public string ToLispString() => Value;

    public override string ToString() => Value;
}
