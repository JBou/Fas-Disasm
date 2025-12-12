using System.Text;

namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a list type in Visual Lisp (opcode 0x39).
/// Lists are the fundamental data structure in Lisp.
/// </summary>
public sealed class FasList : IFasType<List<object?>>
{
    public const byte OpcodeValue = 0x39;

    public string TypeName => "LIST";
    public byte Opcode => OpcodeValue;

    /// <summary>
    /// Gets or sets the list items.
    /// </summary>
    public List<object?> Value { get; set; } = new();

    /// <summary>
    /// Gets the list items (alias for Value).
    /// </summary>
    public List<object?> Items => Value;

    /// <summary>
    /// When true, this list was created with cons (dotted pair).
    /// </summary>
    public bool IsCons { get; set; }

    /// <summary>
    /// When true, the list is quoted (prefixed with ').
    /// </summary>
    public bool IsQuoted { get; set; }

    public FasList() { }

    public FasList(IEnumerable<object?> items)
    {
        Value = new List<object?>(items);
    }

    /// <summary>
    /// Gets an item at the specified index.
    /// </summary>
    public object? this[int index]
    {
        get => Value[index];
        set => Value[index] = value;
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public int Count => Value.Count;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    public void Add(object? item) => Value.Add(item);

    public string ToLispString()
    {
        var prefix = IsQuoted ? "'" : "";
        var funcName = IsCons ? "cons" : "";

        return $"{prefix}{FormatList(funcName, Value)}";
    }

    private static string FormatList(string prefix, IEnumerable<object?> items)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(prefix))
        {
            sb.Append('(');
            sb.Append(prefix);
            sb.Append(' ');
        }
        else
        {
            sb.Append('(');
        }

        var first = true;
        foreach (var item in items)
        {
            if (!first)
                sb.Append(' ');
            first = false;

            sb.Append(FormatItem(item));
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string FormatItem(object? item)
    {
        return item switch
        {
            null => "nil",
            IFasType fasType => fasType.ToLispString(),
            IEnumerable<object?> list => FormatList("", list),
            _ => item.ToString() ?? "nil"
        };
    }

    public override string ToString() => ToLispString();
}
