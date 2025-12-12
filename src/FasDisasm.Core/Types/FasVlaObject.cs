namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a VLA (Visual LISP for Applications) object in Visual Lisp.
/// VLA objects are COM wrappers for AutoCAD objects.
/// </summary>
public sealed class FasVlaObject : IFasType<string>
{
    public string TypeName => "VLA-OBJECT";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the object type/class name.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public FasVlaObject() { }

    public FasVlaObject(string value)
    {
        Value = value;
    }

    public string ToLispString() => $"#<VLA-OBJECT {Value}>";

    public override string ToString() => ToLispString();
}
