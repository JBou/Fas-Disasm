namespace FasDisasm.Core.Disassembly;

/// <summary>
/// A stack implementation for the FAS disassembler execution simulation.
/// </summary>
public class FasStack
{
    private readonly List<object?> _storage = new();

    /// <summary>
    /// Gets or sets the stack pointer (current top of stack index).
    /// </summary>
    public int StackPointer { get; set; }

    /// <summary>
    /// Gets the number of items on the stack.
    /// </summary>
    public int Count => StackPointer;

    /// <summary>
    /// Gets whether the stack is empty.
    /// </summary>
    public bool IsEmpty => StackPointer <= 0;

    /// <summary>
    /// Gets or sets the current top item without modifying the stack.
    /// </summary>
    public object? Current
    {
        get
        {
            if (StackPointer <= 0)
                throw new InvalidOperationException("Stack is empty.");
            return _storage[StackPointer - 1];
        }
        set
        {
            if (StackPointer > 0)
                Pop();
            Push(value);
        }
    }

    /// <summary>
    /// Pushes an item onto the stack.
    /// </summary>
    public void Push(object? item)
    {
        if (StackPointer >= _storage.Count)
        {
            _storage.Add(item);
        }
        else
        {
            _storage[StackPointer] = item;
        }
        StackPointer++;
    }

    /// <summary>
    /// Pops an item from the stack and returns it.
    /// </summary>
    public object? Pop()
    {
        if (StackPointer <= 0)
            throw new InvalidOperationException("Stack is empty - Pop is not possible.");

        StackPointer--;
        return _storage[StackPointer];
    }

    /// <summary>
    /// Pops an item from the stack without returning it.
    /// </summary>
    public void PopVoid()
    {
        if (StackPointer > 0)
            StackPointer--;
    }

    /// <summary>
    /// Peeks at the item at the specified depth (0 = top of stack).
    /// </summary>
    public object? Peek(int depth = 0)
    {
        var index = StackPointer - 1 - depth;
        if (index < 0 || index >= _storage.Count)
            throw new InvalidOperationException($"Invalid stack depth: {depth}");
        return _storage[index];
    }

    /// <summary>
    /// Pops multiple items from the stack and returns them as an array.
    /// Items are returned in the order they were pushed (bottom to top).
    /// </summary>
    public object?[] PopArray(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count == 0)
            return Array.Empty<object?>();

        var result = new object?[count];
        for (int i = count - 1; i >= 0; i--)
        {
            result[i] = Pop();
        }
        return result;
    }

    /// <summary>
    /// Duplicates the top item on the stack.
    /// </summary>
    public void Duplicate()
    {
        if (StackPointer <= 0)
            throw new InvalidOperationException("Stack is empty - cannot duplicate.");
        Push(_storage[StackPointer - 1]);
    }

    /// <summary>
    /// Clears the stack.
    /// </summary>
    public void Clear()
    {
        _storage.Clear();
        StackPointer = 0;
    }

    /// <summary>
    /// Gets a snapshot of the current stack state as a string.
    /// </summary>
    public string GetStackString()
    {
        if (StackPointer <= 0)
            return "[empty]";

        var items = new List<string>();
        for (int i = 0; i < StackPointer; i++)
        {
            var item = _storage[i];
            items.Add(item?.ToString() ?? "nil");
        }
        return $"[{string.Join(", ", items)}]";
    }

    /// <summary>
    /// Creates a copy of the current stack contents.
    /// </summary>
    public object?[] ToArray()
    {
        var result = new object?[StackPointer];
        for (int i = 0; i < StackPointer; i++)
        {
            result[i] = _storage[i];
        }
        return result;
    }
}
