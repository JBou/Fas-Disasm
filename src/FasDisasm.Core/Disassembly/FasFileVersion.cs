namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Supported FAS file format versions.
/// </summary>
public enum FasFileVersion
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown,

    /// <summary>FAS format (basic).</summary>
    Fas,

    /// <summary>FAS2 format.</summary>
    Fas2,

    /// <summary>FAS3 format.</summary>
    Fas3,

    /// <summary>FAS4 format.</summary>
    Fas4,

    /// <summary>FSL format (compiled Lisp).</summary>
    Fsl,

    /// <summary>LTFAS format (AutoCAD LT).</summary>
    LtFas,

    /// <summary>VLX format (Visual Lisp archive).</summary>
    Vlx,

    /// <summary>Protected LSP file.</summary>
    ProtectedLsp
}

/// <summary>
/// File signature constants for FAS files.
/// </summary>
public static class FasFileSignatures
{
    public const string Fas = "FAS-FILE";
    public const string Fas2 = "FAS2-FILE";
    public const string Fas3 = "FAS3-FILE";
    public const string Fas4 = "FAS4-FILE";
    public const string Fsl = "1Y";
    public const string LtFas = "AutoCAD LT OEM Product";
    public const string Vlx = "VRTLIB";

    /// <summary>
    /// Detects the file version from the header bytes.
    /// </summary>
    public static FasFileVersion DetectVersion(ReadOnlySpan<byte> header)
    {
        if (header.Length < 2)
            return FasFileVersion.Unknown;

        var headerStr = System.Text.Encoding.ASCII.GetString(header);

        if (headerStr.StartsWith(Fas4))
            return FasFileVersion.Fas4;
        if (headerStr.StartsWith(Fas3))
            return FasFileVersion.Fas3;
        if (headerStr.StartsWith(Fas2))
            return FasFileVersion.Fas2;
        if (headerStr.StartsWith(Fas))
            return FasFileVersion.Fas;
        if (headerStr.StartsWith(Fsl))
            return FasFileVersion.Fsl;
        if (headerStr.StartsWith(LtFas))
            return FasFileVersion.LtFas;
        if (headerStr.StartsWith(Vlx))
            return FasFileVersion.Vlx;

        return FasFileVersion.Unknown;
    }
}
