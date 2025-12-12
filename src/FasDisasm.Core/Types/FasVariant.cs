namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a Variant type in Visual Lisp.
/// Variants are used for COM interop.
/// </summary>
public sealed class FasVariant : IFasType<object?>
{
    public string TypeName => "VARIANT";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the variant value.
    /// </summary>
    public object? Value { get; set; }

    public FasVariant() { }

    public FasVariant(object? value)
    {
        Value = value;
    }

    public string ToLispString() => $"#<variant {Value?.GetType().Name ?? "null"} {Value}>";

    public override string ToString() => ToLispString();
}
