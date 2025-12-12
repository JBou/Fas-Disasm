using FasDisasm.Core.IO;
using FasDisasm.Core.Types;

namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Reads and parses FAS/FSL files.
/// </summary>
public class FasFileReader : IDisposable
{
    private BinaryStreamReader? _reader;
    private bool _disposed;

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the detected file version.
    /// </summary>
    public FasFileVersion Version { get; private set; }

    /// <summary>
    /// Gets the resource stream data (init/main section).
    /// </summary>
    public byte[] ResourceData { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the function stream data.
    /// </summary>
    public byte[] FunctionData { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the number of resource stream variables.
    /// </summary>
    public int ResourceStreamVars { get; private set; }

    /// <summary>
    /// Gets the number of function stream variables.
    /// </summary>
    public int FunctionStreamVars { get; private set; }

    /// <summary>
    /// Gets the offset where the code section starts.
    /// </summary>
    public long CodeStartOffset { get; private set; }

    /// <summary>
    /// Gets the offset where the data section starts.
    /// </summary>
    public long DataStartOffset { get; private set; }

    /// <summary>
    /// Gets the module variables (two arrays for module 0 and 1).
    /// </summary>
    public object?[][] ModuleVars { get; } = new object?[2][];

    /// <summary>
    /// Event raised during file loading progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? LoadProgress;

    /// <summary>
    /// Event raised during decryption progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? DecryptProgress;

    /// <summary>
    /// Loads a FAS/FSL file.
    /// </summary>
    public void Load(string fileName)
    {
        FileName = fileName;
        _reader = new BinaryStreamReader(fileName);

        // Detect file version
        var header = _reader.PeekBytes(Math.Min(32, (int)_reader.Length));
        Version = FasFileSignatures.DetectVersion(header);

        if (Version == FasFileVersion.Unknown)
        {
            throw new InvalidDataException($"Unknown or unsupported file format: {fileName}");
        }

        // Parse based on version
        switch (Version)
        {
            case FasFileVersion.Fas4:
            case FasFileVersion.Fas3:
            case FasFileVersion.Fas2:
            case FasFileVersion.Fas:
                ParseFasFile();
                break;
            case FasFileVersion.Fsl:
                ParseFslFile();
                break;
            case FasFileVersion.LtFas:
                ParseLtFasFile();
                break;
            case FasFileVersion.Vlx:
                throw new NotSupportedException("VLX files should be extracted first using VlxSplitter.");
            default:
                throw new NotSupportedException($"File version {Version} is not yet supported.");
        }
    }

    private void ParseFasFile()
    {
        if (_reader == null) return;

        // Skip the signature line (e.g., "FAS4-FILE ; Do not change it!")
        var line = ReadLine();

        // Read the encryption key line if present
        line = ReadLine();

        // Parse stream information
        ParseStreamInfo();

        // Decrypt if necessary
        DecryptResourceData();
    }

    private void ParseFslFile()
    {
        if (_reader == null) return;

        // FSL format starts with "1Y"
        _reader.Skip(2);

        // Parse FSL-specific structure
        ParseStreamInfo();
    }

    private void ParseLtFasFile()
    {
        if (_reader == null) return;

        // LTFAS has a different header structure
        var signature = _reader.ReadFixedString(22);

        // Parse the rest similar to FAS
        ParseStreamInfo();
        DecryptResourceData();
    }

    private void ParseStreamInfo()
    {
        if (_reader == null) return;

        // Skip whitespace and read stream lengths
        SkipWhitespace();

        // Read resource stream length
        var resourceLengthStr = ReadNumber();
        if (int.TryParse(resourceLengthStr, out var resourceLength))
        {
            // Read resource stream vars
            SkipWhitespace();
            var resourceVarsStr = ReadNumber();
            if (int.TryParse(resourceVarsStr, out var resourceVars))
            {
                ResourceStreamVars = resourceVars;
            }
        }

        // Skip to data section
        SkipWhitespace();
        SkipStreamDelimiter();

        // Store code start offset
        CodeStartOffset = _reader.Position;

        // Read resource data
        if (resourceLength > 0 && _reader.Remaining >= resourceLength)
        {
            ResourceData = _reader.ReadBytes(resourceLength);
        }

        // Read function stream info if present
        SkipWhitespace();
        var funcLengthStr = ReadNumber();
        if (int.TryParse(funcLengthStr, out var funcLength))
        {
            SkipWhitespace();
            var funcVarsStr = ReadNumber();
            if (int.TryParse(funcVarsStr, out var funcVars))
            {
                FunctionStreamVars = funcVars;
            }

            SkipWhitespace();
            SkipStreamDelimiter();

            DataStartOffset = _reader.Position;

            if (funcLength > 0 && _reader.Remaining >= funcLength)
            {
                FunctionData = _reader.ReadBytes(funcLength);
            }
        }

        // Initialize module variables
        ModuleVars[0] = new object?[ResourceStreamVars];
        ModuleVars[1] = new object?[FunctionStreamVars];
    }

    private void DecryptResourceData()
    {
        if (ResourceData.Length == 0) return;

        // FAS files use a simple XOR-based encryption
        // The decryption key is typically embedded in the file header

        OnDecryptProgress(0, ResourceData.Length);

        // Simple decryption (the actual algorithm varies by version)
        // This is a placeholder - the real decryption is more complex
        for (int i = 0; i < ResourceData.Length; i++)
        {
            // Decryption would happen here based on the key
            // ResourceData[i] ^= key[i % key.Length];

            if (i % 1000 == 0)
            {
                OnDecryptProgress(i, ResourceData.Length);
            }
        }

        OnDecryptProgress(ResourceData.Length, ResourceData.Length);
    }

    private string ReadLine()
    {
        if (_reader == null) return string.Empty;

        var chars = new List<char>();
        while (!_reader.EndOfStream)
        {
            var b = _reader.ReadByte();
            if (b == '\n') break;
            if (b != '\r')
                chars.Add((char)b);
        }
        return new string(chars.ToArray());
    }

    private void SkipWhitespace()
    {
        if (_reader == null) return;

        while (!_reader.EndOfStream)
        {
            var b = _reader.PeekByte();
            if (b != ' ' && b != '\t' && b != '\r' && b != '\n')
                break;
            _reader.Skip(1);
        }
    }

    private string ReadNumber()
    {
        if (_reader == null) return string.Empty;

        var chars = new List<char>();
        while (!_reader.EndOfStream)
        {
            var b = _reader.PeekByte();
            if (b < '0' || b > '9')
                break;
            chars.Add((char)_reader.ReadByte());
        }
        return new string(chars.ToArray());
    }

    private void SkipStreamDelimiter()
    {
        if (_reader == null) return;

        // Skip the stream delimiter character (usually '!' followed by another char)
        if (!_reader.EndOfStream && _reader.PeekByte() == '!')
        {
            _reader.Skip(1);
            if (!_reader.EndOfStream)
                _reader.Skip(1);
        }
    }

    /// <summary>
    /// Creates a stream reader for the resource data.
    /// </summary>
    public BinaryStreamReader CreateResourceStream()
    {
        return new BinaryStreamReader(ResourceData);
    }

    /// <summary>
    /// Creates a stream reader for the function data.
    /// </summary>
    public BinaryStreamReader CreateFunctionStream()
    {
        return new BinaryStreamReader(FunctionData);
    }

    protected virtual void OnLoadProgress(long current, long total)
    {
        LoadProgress?.Invoke(this, new ProgressEventArgs(current, total));
    }

    protected virtual void OnDecryptProgress(long current, long total)
    {
        DecryptProgress?.Invoke(this, new ProgressEventArgs(current, total));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _reader?.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Event arguments for progress reporting.
/// </summary>
public class ProgressEventArgs : EventArgs
{
    public long Current { get; }
    public long Total { get; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;

    public ProgressEventArgs(long current, long total)
    {
        Current = current;
        Total = total;
    }
}
