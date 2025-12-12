namespace FasDisasm.Core.Types;

/// <summary>
/// Base interface for all FAS (Visual Lisp) data types.
/// </summary>
public interface IFasType
{
    /// <summary>
    /// Gets the Lisp type name (e.g., "INT", "STR", "LIST", etc.)
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Gets the opcode associated with this type.
    /// </summary>
    byte Opcode { get; }

    /// <summary>
    /// Converts the value to its Lisp text representation.
    /// </summary>
    string ToLispString();
}

/// <summary>
/// Base interface for FAS types that hold a value.
/// </summary>
public interface IFasType<T> : IFasType
{
    /// <summary>
    /// Gets or sets the underlying value.
    /// </summary>
    T Value { get; set; }
}
