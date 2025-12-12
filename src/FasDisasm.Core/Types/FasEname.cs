namespace FasDisasm.Core.Types;

/// <summary>
/// Represents an entity name (ENAME) type in Visual Lisp.
/// ENAMEs are handles to AutoCAD entities.
/// </summary>
public sealed class FasEname : IFasType<long>
{
    public string TypeName => "ENAME";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the entity name value.
    /// </summary>
    public long Value { get; set; }

    public FasEname() { }

    public FasEname(long value)
    {
        Value = value;
    }

    public string ToLispString() => $"<Entity name: {Value:x}>";

    public override string ToString() => ToLispString();
}
