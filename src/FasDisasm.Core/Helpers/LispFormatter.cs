using System.Text;
using FasDisasm.Core.Types;

namespace FasDisasm.Core.Helpers;

/// <summary>
/// Helper class for formatting Lisp expressions.
/// </summary>
public static class LispFormatter
{
    /// <summary>
    /// Formats a function call as a Lisp expression.
    /// </summary>
    public static string FormatFunctionCall(string functionName, params object?[] args)
    {
        if (string.IsNullOrEmpty(functionName) && args.Length == 0)
            return "()";

        var sb = new StringBuilder();
        sb.Append('(');

        if (!string.IsNullOrEmpty(functionName))
        {
            sb.Append(functionName);
        }

        foreach (var arg in args)
        {
            if (sb.Length > 1)
                sb.Append(' ');
            sb.Append(FormatValue(arg));
        }

        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Formats a value for Lisp output.
    /// </summary>
    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => "nil",
            IFasType fasType => fasType.ToLispString(),
            string s => s,
            IEnumerable<object?> enumerable => FormatList(enumerable),
            _ => value.ToString() ?? "nil"
        };
    }

    /// <summary>
    /// Formats a list of values.
    /// </summary>
    public static string FormatList(IEnumerable<object?> items)
    {
        var sb = new StringBuilder();
        sb.Append('(');

        var first = true;
        foreach (var item in items)
        {
            if (!first)
                sb.Append(' ');
            first = false;
            sb.Append(FormatValue(item));
        }

        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Joins multiple values into a single Lisp expression.
    /// </summary>
    public static string Join(string separator, params object?[] values)
    {
        return string.Join(separator, values.Select(FormatValue));
    }

    /// <summary>
    /// Quotes a string for Lisp output.
    /// </summary>
    public static string Quote(string value)
    {
        return $"\"{EscapeString(value)}\"";
    }

    /// <summary>
    /// Removes quotes from a string.
    /// </summary>
    public static string Unquote(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return UnescapeString(value[1..^1]);
        }
        return value;
    }

    /// <summary>
    /// Escapes special characters in a string.
    /// </summary>
    public static string EscapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\x1B':
                    sb.Append("\\e");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Unescapes special characters in a string.
    /// </summary>
    public static string UnescapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                i++;
                switch (value[i])
                {
                    case '\\':
                        sb.Append('\\');
                        break;
                    case '"':
                        sb.Append('"');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'e':
                        sb.Append('\x1B');
                        break;
                    default:
                        sb.Append('\\');
                        sb.Append(value[i]);
                        break;
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates an indentation string.
    /// </summary>
    public static string Indent(int level, int spacesPerLevel = 2)
    {
        return new string(' ', level * spacesPerLevel);
    }

    /// <summary>
    /// Formats an offset as a hexadecimal string.
    /// </summary>
    public static string FormatOffset(long offset)
    {
        return $"${offset:X4}";
    }

    /// <summary>
    /// Formats a byte as a hexadecimal string.
    /// </summary>
    public static string FormatByte(byte value)
    {
        return $"{value:X2}";
    }
}
