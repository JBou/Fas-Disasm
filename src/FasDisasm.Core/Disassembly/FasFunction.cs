using FasDisasm.Core.Types;

namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Represents a function definition in a FAS file.
/// </summary>
public class FasFunction
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a lambda (anonymous) function.
    /// </summary>
    public bool IsLambda { get; set; }

    /// <summary>
    /// Gets or sets the start offset in the stream.
    /// </summary>
    public long StartOffset { get; set; }

    /// <summary>
    /// Gets or sets the end offset in the stream.
    /// </summary>
    public long EndOffset { get; set; }

    /// <summary>
    /// Gets or sets the module ID (0 = init/main, 1 = functions).
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the number of arguments.
    /// </summary>
    public int ArgumentCount { get; set; }

    /// <summary>
    /// Gets or sets the argument names.
    /// </summary>
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of local variables.
    /// </summary>
    public int LocalVariableCount { get; set; }

    /// <summary>
    /// Gets or sets the local variables.
    /// </summary>
    public List<FasLocalVar> LocalVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets additional variable storage.
    /// </summary>
    public object? Vars { get; set; }

    /// <summary>
    /// Initializes local variables with default placeholder names.
    /// </summary>
    /// <param name="count">Number of local variables.</param>
    /// <param name="prefix">Prefix for uninitialized variable names.</param>
    public void InitializeLocalVariables(int count, string prefix = "_fas")
    {
        LocalVariableCount = count;
        LocalVariables.Clear();

        for (int i = 0; i <= count; i++)
        {
            LocalVariables.Add(new FasLocalVar(i, $"{prefix}{i}"));
        }
    }

    /// <summary>
    /// Gets a local variable by index.
    /// </summary>
    public FasLocalVar GetLocalVariable(int index)
    {
        if (index < 0 || index >= LocalVariables.Count)
        {
            // Return a placeholder if index is out of range
            return new FasLocalVar(index);
        }
        return LocalVariables[index];
    }

    /// <summary>
    /// Sets a local variable's name by index.
    /// </summary>
    public void SetLocalVariable(int index, string name)
    {
        // Ensure we have enough local variables
        while (LocalVariables.Count <= index)
        {
            LocalVariables.Add(new FasLocalVar(LocalVariables.Count));
        }
        LocalVariables[index].Value = name;
    }

    /// <summary>
    /// Gets the function signature as a Lisp string.
    /// </summary>
    public string GetSignature()
    {
        var args = Arguments.Count > 0 ? string.Join(" ", Arguments) : "";
        var locals = LocalVariables.Count > 0
            ? " / " + string.Join(" ", LocalVariables.Select(v => v.Value))
            : "";

        if (IsLambda)
        {
            return $"(lambda ({args}{locals}) ...)";
        }
        return $"(defun {Name} ({args}{locals}) ...)";
    }

    public override string ToString()
    {
        return $"{Name} @ ${StartOffset:X} [{Arguments.Count} args, {LocalVariableCount} locals]";
    }
}
