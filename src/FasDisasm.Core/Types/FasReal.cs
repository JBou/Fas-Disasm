using System.Globalization;

namespace FasDisasm.Core.Types;

/// <summary>
/// Represents a floating-point number type in Visual Lisp (opcode 0x3B).
/// </summary>
public sealed class FasReal : IFasType<double>
{
    public const byte OpcodeValue = 0x3B;

    public string TypeName => "REAL";
    public byte Opcode => OpcodeValue;

    /// <summary>
    /// Gets or sets the floating-point value.
    /// </summary>
    public double Value { get; set; }

    public FasReal() { }

    public FasReal(double value)
    {
        Value = value;
    }

    public string ToLispString()
    {
        var str = Value.ToString("G", CultureInfo.InvariantCulture);
        // Ensure decimal point is present for Lisp compatibility
        if (!str.Contains('.') && !str.Contains('E') && !str.Contains('e'))
        {
            str += ".0";
        }
        return str;
    }

    public override string ToString() => ToLispString();
}
