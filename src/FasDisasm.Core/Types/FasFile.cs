namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a file handle type in Visual Lisp.
/// </summary>
public sealed class FasFileHandle : IFasType<string>
{
    public string TypeName => "FILE";
    public byte Opcode => 0x00;

    /// <summary>
    /// Gets or sets the file descriptor/path.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public FasFileHandle() { }

    public FasFileHandle(string value)
    {
        Value = value;
    }

    public string ToLispString() => $"#<file \"{Value}\">";

    public override string ToString() => ToLispString();
}
