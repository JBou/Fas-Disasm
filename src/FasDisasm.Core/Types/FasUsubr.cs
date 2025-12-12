namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a user-defined subroutine (USUBR) in Visual Lisp (opcode 0x35).
/// USUBRs are functions defined by defun or lambda.
/// </summary>
public sealed class FasUsubr : IFasType<string>
{
    public const byte OpcodeValue = 0x35;

    public string TypeName => "USUBR";
    public byte Opcode => OpcodeValue;

    /// <summary>
    /// Gets or sets the function name (or "-lambda-" for anonymous functions).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function name (alias for Value).
    /// </summary>
    public string Name
    {
        get => Value;
        set => Value = value;
    }

    /// <summary>
    /// Gets or sets the stream offset where this function is defined.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Gets or sets the module ID (0 = init/main, 1 = functions).
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the local variable reference (if applicable).
    /// </summary>
    public FasLocalVar? LocalVar { get; set; }

    /// <summary>
    /// When true, this is an anonymous lambda function.
    /// </summary>
    public bool IsLambda => Value == "-lambda-" || string.IsNullOrEmpty(Value);

    public FasUsubr() { }

    public FasUsubr(string name, long offset, int moduleId = 0)
    {
        Value = name;
        Offset = offset;
        ModuleId = moduleId;
    }

    public string ToLispString()
    {
        var lvarStr = LocalVar != null ? $"Modul:{LocalVar}" : "";
        return $"<Func> {Value} {lvarStr}, Offs: ${Offset:X} [{Offset}]";
    }

    public override string ToString() => ToLispString();
}
