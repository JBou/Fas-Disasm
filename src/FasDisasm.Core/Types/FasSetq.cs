namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a SETQ (assignment) expression in Visual Lisp.
/// </summary>
public sealed class FasSetq : IFasType
{
    public string TypeName => "SETQ";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the variable name being assigned.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value being assigned.
    /// </summary>
    public object? Value { get; set; }

    public FasSetq() { }

    public FasSetq(string variableName, object? value)
    {
        VariableName = variableName;
        Value = value;
    }

    public string ToLispString()
    {
        var valueStr = Value switch
        {
            null => "nil",
            IFasType fasType => fasType.ToLispString(),
            _ => Value.ToString() ?? "nil"
        };
        return $"(setq {VariableName} {valueStr})";
    }

    public override string ToString() => ToLispString();
}
