namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a SafeArray type in Visual Lisp.
/// SafeArrays are used for COM interop.
/// </summary>
public sealed class FasSafeArray : IFasType<object?[]>
{
    public string TypeName => "SAFEARRAY";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the array elements.
    /// </summary>
    public object?[] Value { get; set; } = Array.Empty<object?>();

    public FasSafeArray() { }

    public FasSafeArray(object?[] value)
    {
        Value = value;
    }

    public string ToLispString()
    {
        var elements = string.Join(" ", Value.Select(v => v?.ToString() ?? "nil"));
        return $"#<safearray...>";
    }

    public override string ToString() => ToLispString();
}
