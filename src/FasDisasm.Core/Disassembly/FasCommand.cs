namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Represents a single disassembled FAS command/instruction.
/// </summary>
public class FasCommand
{
    /// <summary>
    /// Gets or sets the module ID (0 = init/main, 1 = functions).
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the byte position/offset of this command in the stream.
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Gets or sets the opcode byte.
    /// </summary>
    public byte Opcode { get; set; }

    /// <summary>
    /// Gets or sets the stack pointer before this command executes.
    /// </summary>
    public int StackPointerBefore { get; set; }

    /// <summary>
    /// Gets or sets the stack pointer after this command executes.
    /// </summary>
    public int StackPointerAfter { get; set; }

    /// <summary>
    /// Gets or sets a snapshot of the stack before this command.
    /// </summary>
    public string? StackBefore { get; set; }

    /// <summary>
    /// Gets or sets a snapshot of the stack after this command.
    /// </summary>
    public string? StackAfter { get; set; }

    /// <summary>
    /// Gets or sets the parameter bytes for this command.
    /// </summary>
    public List<byte> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the disassembled text representation.
    /// </summary>
    public string Disassembled { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a short form of the disassembled text.
    /// </summary>
    public string DisassembledShort { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of this command.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decompiled/interpreted Lisp code.
    /// </summary>
    public string Interpreted { get; set; } = string.Empty;

    /// <summary>
    /// Gets the offset as a formatted hex string.
    /// </summary>
    public string OffsetHex => $"${Position:X4}";

    /// <summary>
    /// Gets the opcode as a formatted hex string.
    /// </summary>
    public string OpcodeHex => $"{Opcode:X2}";

    /// <summary>
    /// Gets the parameters as a formatted hex string.
    /// </summary>
    public string ParametersHex => string.Join(" ", Parameters.Select(p => $"{p:X2}"));

    public override string ToString()
    {
        return $"{OffsetHex}: {OpcodeHex} {ParametersHex,-12} {Disassembled}";
    }
}
