using System.Text;

namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a string type in Visual Lisp (opcode 0x55).
/// </summary>
public sealed class FasString : IFasType<string>
{
    public const byte OpcodeValue = 0x55;

    public string TypeName => "STR";
    public byte Opcode => OpcodeValue;

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public FasString() { }

    public FasString(string value)
    {
        Value = value;
    }

    public string ToLispString() => $"\"{Escape(Value)}\"";

    /// <summary>
    /// Escapes special characters in a string for Lisp output.
    /// </summary>
    private static string Escape(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length * 2);

        foreach (var c in input)
        {
            switch (c)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\x1B': // Escape character
                    sb.Append("\\e");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    public override string ToString() => ToLispString();
}
