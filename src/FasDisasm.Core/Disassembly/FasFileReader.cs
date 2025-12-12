using FasDisasm.Core.IO;
using FasDisasm.Core.Types;
using System.Text;

namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Reads and parses FAS/FSL files.
/// Ported from the original VB6 FasFile.cls.
/// </summary>
public class FasFileReader : IDisposable
{
    // Whitespace table constants (from VB6 FasFile.cls)
    private const int WS_CONTROLCHAR = 0;      // Chr(0..8, b, e, f)
    private const int WS_WHITESPACE = 1;       // Tab, LF, NewLine, CR, 1A, Space
    private const int WS_BLACKSLASH = 4;       // \
    private const int WS_PIPE = 5;             // |
    private const int WS_ALPHANUMERIC = 0xA;   // 0-9, A-Z, a-z, '-', '_', etc.
    private const int WS_LIMITER = 0xB;        // !"'(),;?{}~
    private const int WS_DASH = 0xF;           // #

    // File signatures
    private const string FAS4_FILE_SIGNATURE = "FAS4-FILE";
    private const string FAS3_FILE_SIGNATURE = "FAS3-FILE";
    private const string FAS2_FILE_SIGNATURE = "FAS2-FILE";
    private const string FAS_FILE_SIGNATURE = "FAS-FILE";
    private const string LTFAS_FILE_SIGNATURE = "AutoCAD LT OEM Product";
    private const string FSL_FILE_SIGNATURE = "1Y";

    private BinaryStreamReader? _reader;
    private byte[] _fileData = Array.Empty<byte>();
    private long _position;
    private bool _disposed;

    // Whitespace table (initialized in constructor)
    private readonly int[] _whitespaceTable = new int[256];

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
    /// Gets whether the file was encrypted.
    /// </summary>
    public bool WasEncrypted { get; private set; }

    /// <summary>
    /// Gets the encryption key used (if file was encrypted).
    /// </summary>
    public byte[] EncryptionKey { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Event raised during file loading progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? LoadProgress;

    /// <summary>
    /// Event raised during decryption progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? DecryptProgress;

    /// <summary>
    /// Creates a new FasFileReader instance.
    /// </summary>
    public FasFileReader()
    {
        InitializeWhitespaceTable();
    }

    /// <summary>
    /// Initializes the whitespace table (mimics VB6 VL_WHITESPACE_TABLE).
    /// </summary>
    private void InitializeWhitespaceTable()
    {
        // Default: all are control characters
        for (int i = 0; i < 256; i++)
        {
            _whitespaceTable[i] = WS_CONTROLCHAR;
        }

        // Whitespace: Tab(9), LF(10), CR(13), Space(32), 0x1A
        _whitespaceTable[9] = WS_WHITESPACE;   // Tab
        _whitespaceTable[10] = WS_WHITESPACE;  // LF
        _whitespaceTable[11] = WS_WHITESPACE;  // VT
        _whitespaceTable[12] = WS_WHITESPACE;  // FF
        _whitespaceTable[13] = WS_WHITESPACE;  // CR
        _whitespaceTable[26] = WS_WHITESPACE;  // 0x1A
        _whitespaceTable[32] = WS_WHITESPACE;  // Space

        // Alphanumeric: 0-9, A-Z, a-z, and some special chars like -, _, etc.
        for (int i = '0'; i <= '9'; i++)
            _whitespaceTable[i] = WS_ALPHANUMERIC;
        for (int i = 'A'; i <= 'Z'; i++)
            _whitespaceTable[i] = WS_ALPHANUMERIC;
        for (int i = 'a'; i <= 'z'; i++)
            _whitespaceTable[i] = WS_ALPHANUMERIC;
        _whitespaceTable['-'] = WS_ALPHANUMERIC;
        _whitespaceTable['_'] = WS_ALPHANUMERIC;

        // Limiters: !"'(),;?{}~
        _whitespaceTable['!'] = WS_LIMITER;
        _whitespaceTable['"'] = WS_LIMITER;
        _whitespaceTable['\''] = WS_LIMITER;
        _whitespaceTable['('] = WS_LIMITER;
        _whitespaceTable[')'] = WS_LIMITER;
        _whitespaceTable[','] = WS_LIMITER;
        _whitespaceTable[';'] = WS_LIMITER;
        _whitespaceTable['?'] = WS_LIMITER;
        _whitespaceTable['{'] = WS_LIMITER;
        _whitespaceTable['}'] = WS_LIMITER;
        _whitespaceTable['~'] = WS_LIMITER;

        // Special characters
        _whitespaceTable['\\'] = WS_BLACKSLASH;
        _whitespaceTable['|'] = WS_PIPE;
        _whitespaceTable['#'] = WS_DASH;
    }

    /// <summary>
    /// Loads a FAS/FSL file.
    /// </summary>
    public void Load(string fileName)
    {
        FileName = fileName;
        _fileData = File.ReadAllBytes(fileName);
        _position = 0;

        Console.WriteLine($"[FasFileReader] Loading file: {fileName}");
        Console.WriteLine($"[FasFileReader] File size: {_fileData.Length} bytes");

        // Check for LT-FAS file
        if (_fileData.Length >= LTFAS_FILE_SIGNATURE.Length)
        {
            var ltfasHeader = Encoding.ASCII.GetString(_fileData, 0, LTFAS_FILE_SIGNATURE.Length);
            if (ltfasHeader == LTFAS_FILE_SIGNATURE)
            {
                throw new NotSupportedException("LT-FAS Format is not supported yet.");
            }
        }

        // Reset position
        _position = 0;

        // Skip leading whitespace and get first non-whitespace character
        char tmpChar = SkipWhitespaceEx();
        Console.WriteLine($"[FasFileReader] First non-whitespace char: '{tmpChar}' (0x{(int)tmpChar:X2}) at position {_position - 1}");

        // Determine if it's FAS or FSL
        bool isFsl = _whitespaceTable[(byte)tmpChar] == WS_DASH;
        Console.WriteLine($"[FasFileReader] Is FSL: {isFsl}");

        if (isFsl)
        {
            ParseFslFile(tmpChar);
        }
        else
        {
            ParseFasFile(tmpChar);
        }

        // Initialize module variables
        ModuleVars[0] = new object?[ResourceStreamVars];
        ModuleVars[1] = new object?[FunctionStreamVars];

        Console.WriteLine($"[FasFileReader] Resource stream: {ResourceData.Length} bytes, {ResourceStreamVars} vars");
        Console.WriteLine($"[FasFileReader] Function stream: {FunctionData.Length} bytes, {FunctionStreamVars} vars");
    }

    private void ParseFasFile(char firstChar)
    {
        Console.WriteLine("[FasFileReader] Parsing FAS file...");

        // Read signature (all following alphanumeric chars)
        var sigBuilder = new StringBuilder();
        char tmpChar = firstChar;

        while (_whitespaceTable[(byte)tmpChar] == WS_ALPHANUMERIC && _position < 1024)
        {
            sigBuilder.Append(tmpChar);
            tmpChar = ReadChar();
        }

        // Rewind by one byte (we read one past the signature)
        _position--;

        var fileSig = sigBuilder.ToString();
        Console.WriteLine($"[FasFileReader] File signature: {fileSig}");

        // Determine version
        Version = fileSig switch
        {
            FAS4_FILE_SIGNATURE => FasFileVersion.Fas4,
            FAS3_FILE_SIGNATURE => FasFileVersion.Fas3,
            FAS2_FILE_SIGNATURE => FasFileVersion.Fas2,
            FAS_FILE_SIGNATURE => FasFileVersion.Fas,
            _ => throw new InvalidDataException($"Invalid FAS file. Signature '{fileSig}' not recognized.")
        };

        Console.WriteLine($"[FasFileReader] Detected version: {Version}");

        // Load function stream first (as in original VB6 code)
        var functionStreamLength = 0;
        FasStreamLoad(ref FunctionData, ".fct", ref functionStreamLength, ref FunctionStreamVars, ref CodeStartOffset);
        Console.WriteLine($"[FasFileReader] Function stream loaded: {functionStreamLength} bytes");

        // Load resource stream
        var resourceStreamLength = 0;
        FasStreamLoad(ref ResourceData, ".res", ref resourceStreamLength, ref ResourceStreamVars, ref DataStartOffset);
        Console.WriteLine($"[FasFileReader] Resource stream loaded: {resourceStreamLength} bytes");
    }

    private void ParseFslFile(char firstChar)
    {
        Console.WriteLine("[FasFileReader] Parsing FSL file...");
        Version = FasFileVersion.Fsl;

        // Read until '#' to get signature
        var sig = GetTerminatedString("#");
        Console.WriteLine($"[FasFileReader] FSL signature part: {sig}");

        if (sig != FSL_FILE_SIGNATURE)
        {
            throw new InvalidDataException($"Invalid FSL file. Signature '{sig}' not recognized.");
        }

        // Load function stream
        var functionStreamLength = 0;
        FslStreamLoad(ref FunctionData, ".fct", ref functionStreamLength, ref FunctionStreamVars, ref CodeStartOffset);
        Console.WriteLine($"[FasFileReader] Function stream loaded: {functionStreamLength} bytes");

        // Load resource stream
        var resourceStreamLength = 0;
        FslStreamLoad(ref ResourceData, ".res", ref resourceStreamLength, ref ResourceStreamVars, ref DataStartOffset);
        Console.WriteLine($"[FasFileReader] Resource stream loaded: {resourceStreamLength} bytes");
    }

    /// <summary>
    /// FAS stream loading (matches VB6 FASStreamLoad).
    /// </summary>
    private void FasStreamLoad(ref byte[] outStream, string ext, ref int streamLength, ref int streamVars, ref long offsetStart)
    {
        // 1. Get stream length
        char tmpChar = SkipWhitespaceEx();
        if (!char.IsDigit(tmpChar))
        {
            throw new InvalidDataException($"Invalid file format - Could not get stream length. Found '{tmpChar}' at position {_position}");
        }

        var lengthStr = new StringBuilder();
        while (char.IsDigit(tmpChar) && !EndOfStream)
        {
            lengthStr.Append(tmpChar);
            tmpChar = ReadChar();
        }
        _position--; // Rewind

        streamLength = int.Parse(lengthStr.ToString());
        Console.WriteLine($"[FasFileReader] Stream length: {streamLength}");

        // 2. Get stream data
        GetStreamData(streamLength, ref streamVars, ref outStream, ext, ref offsetStart);
    }

    /// <summary>
    /// FSL stream loading.
    /// </summary>
    private void FslStreamLoad(ref byte[] outStream, string ext, ref int streamLength, ref int streamVars, ref long offsetStart)
    {
        // FSL format: #length#vars!<data>!
        var lengthStr = GetTerminatedString("#");
        streamLength = int.Parse(lengthStr);
        Console.WriteLine($"[FasFileReader] FSL stream length: {streamLength}");

        var varsStr = GetTerminatedString("#");
        // vars might have 'm' suffix
        if (varsStr.EndsWith("m"))
            varsStr = varsStr.TrimEnd('m');
        streamVars = int.Parse(varsStr);
        Console.WriteLine($"[FasFileReader] FSL stream vars: {streamVars}");

        // Read stream terminator char
        var terminatorChar = ReadChar();
        Console.WriteLine($"[FasFileReader] FSL terminator: '{terminatorChar}'");

        // Store code start offset
        offsetStart = _position;

        // Read stream data
        if (streamLength > 0 && _position + streamLength <= _fileData.Length)
        {
            outStream = new byte[streamLength];
            Array.Copy(_fileData, _position, outStream, 0, streamLength);
            _position += streamLength;
        }

        // Skip terminator
        if (!EndOfStream)
        {
            var endChar = ReadChar();
            Console.WriteLine($"[FasFileReader] FSL end char: '{endChar}'");
        }
    }

    /// <summary>
    /// Gets stream data and handles decryption (matches VB6 getStreamData).
    /// </summary>
    private void GetStreamData(int dataLength, ref int streamVars, ref byte[] outStream, string ext, ref long offsetStart)
    {
        // Get stream vars count
        char tmpChar = SkipWhitespaceEx();
        if (!char.IsDigit(tmpChar))
        {
            throw new InvalidDataException($"Invalid file format - Could not get number of StreamVars. Found '{tmpChar}'");
        }

        var varsStr = new StringBuilder();
        while (char.IsDigit(tmpChar) && !EndOfStream)
        {
            varsStr.Append(tmpChar);
            tmpChar = ReadChar();
        }
        _position--; // Rewind
        streamVars = int.Parse(varsStr.ToString());
        Console.WriteLine($"[FasFileReader] Stream vars: {streamVars}");

        // Get stream begin char (and store it as terminator)
        tmpChar = SkipWhitespace();
        char streamTerminatorChar;
        if (tmpChar == '!')
        {
            streamTerminatorChar = ReadChar();
        }
        else
        {
            streamTerminatorChar = tmpChar;
        }
        Console.WriteLine($"[FasFileReader] Stream terminator char: '{streamTerminatorChar}' (0x{(int)streamTerminatorChar:X2})");

        // Store start of code
        var codeStart = _position;
        offsetStart = codeStart;
        Console.WriteLine($"[FasFileReader] Code start offset: 0x{codeStart:X}");

        // Read the raw stream data
        if (dataLength > 0 && _position + dataLength <= _fileData.Length)
        {
            outStream = new byte[dataLength];
            Array.Copy(_fileData, _position, outStream, 0, dataLength);
            _position += dataLength;
        }
        else
        {
            Console.WriteLine($"[FasFileReader] WARNING: Data length {dataLength} exceeds available data");
            outStream = Array.Empty<byte>();
            return;
        }

        // Get next char to check for encryption
        if (EndOfStream)
        {
            Console.WriteLine("[FasFileReader] End of stream reached");
            return;
        }

        tmpChar = ReadChar();
        Console.WriteLine($"[FasFileReader] Char after data: '{tmpChar}' (0x{(int)tmpChar:X2})");

        // Check if encrypted
        bool isEncrypted = tmpChar != streamTerminatorChar;
        Console.WriteLine($"[FasFileReader] Is encrypted: {isEncrypted}");

        if (isEncrypted)
        {
            WasEncrypted = true;

            // Key length is the char we just read
            int keyLength = (byte)tmpChar;
            Console.WriteLine($"[FasFileReader] Key length: {keyLength}");

            if (keyLength >= 0x80)
            {
                throw new InvalidDataException("Crunch password too long - Key length is bigger than 128 bytes!");
            }

            long keyStart = _position;

            // Read key
            EncryptionKey = new byte[keyLength];
            Array.Copy(_fileData, _position, EncryptionKey, 0, keyLength);
            _position += keyLength;
            Console.WriteLine($"[FasFileReader] Key read: {BitConverter.ToString(EncryptionKey)}");

            // Read char after key
            tmpChar = ReadChar();
            Console.WriteLine($"[FasFileReader] Char after key: '{tmpChar}' (0x{(int)tmpChar:X2})");

            // Decrypt the data
            DecryptData(ref outStream, EncryptionKey);

            // Write decrypted file
            WriteDecryptedFile(FileName + ext, outStream);
        }

        if (tmpChar != streamTerminatorChar)
        {
            Console.WriteLine($"[FasFileReader] WARNING: Stream terminator mismatch. Expected '{streamTerminatorChar}', got '{tmpChar}'");
        }
    }

    /// <summary>
    /// Decrypts the data using XOR algorithm (matches VB6 decryption).
    /// Algorithm: value = data XOR KeyNew XOR KeyOld
    /// </summary>
    private void DecryptData(ref byte[] data, byte[] key)
    {
        if (key.Length == 0 || key[0] == 0)
        {
            Console.WriteLine("[FasFileReader] Key is empty or starts with 0, skipping decryption");
            return;
        }

        Console.WriteLine($"[FasFileReader] Decrypting {data.Length} bytes...");
        OnDecryptProgress(0, data.Length);

        byte keyOld = key[0];
        int keyPos = 0;

        for (int i = 0; i < data.Length; i++)
        {
            // Move to next key byte
            keyPos++;
            if (keyPos >= key.Length)
                keyPos = 0;

            byte keyNew = key[keyPos];

            // Decrypt: value = data XOR keyNew XOR keyOld
            data[i] = (byte)(data[i] ^ keyNew ^ keyOld);

            keyOld = keyNew;

            if (i % 10000 == 0)
            {
                OnDecryptProgress(i, data.Length);
            }
        }

        OnDecryptProgress(data.Length, data.Length);
        Console.WriteLine("[FasFileReader] Decryption complete");
    }

    /// <summary>
    /// Writes the decrypted stream to a file.
    /// </summary>
    private void WriteDecryptedFile(string filePath, byte[] data)
    {
        try
        {
            File.WriteAllBytes(filePath, data);
            Console.WriteLine($"[FasFileReader] Decrypted data written to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FasFileReader] Failed to write decrypted file: {ex.Message}");
        }
    }

    #region Reading helpers

    private bool EndOfStream => _position >= _fileData.Length;

    private char ReadChar()
    {
        if (_position >= _fileData.Length)
            return '\0';
        return (char)_fileData[_position++];
    }

    private byte ReadByte()
    {
        if (_position >= _fileData.Length)
            return 0;
        return _fileData[_position++];
    }

    /// <summary>
    /// Skips whitespace characters.
    /// </summary>
    private char SkipWhitespace()
    {
        char c;
        do
        {
            c = ReadChar();
        } while (_whitespaceTable[(byte)c] == WS_WHITESPACE && !EndOfStream);
        return c;
    }

    /// <summary>
    /// Skips whitespace and comments (lines starting with ';').
    /// </summary>
    private char SkipWhitespaceEx()
    {
        // Skip leading whitespaces
        char c = SkipWhitespace();

        // If first char is ';', skip the comment line
        while (c == ';')
        {
            // Seek for CR or LF
            while (!EndOfStream && c != '\r' && c != '\n')
            {
                c = ReadChar();
            }

            // Skip following whitespaces
            c = SkipWhitespace();
        }

        return c;
    }

    /// <summary>
    /// Reads a string until one of the terminator characters is found.
    /// </summary>
    private string GetTerminatedString(params string[] terminators)
    {
        var sb = new StringBuilder();
        while (!EndOfStream)
        {
            char c = ReadChar();
            string cs = c.ToString();
            if (terminators.Any(t => t == cs))
                break;
            sb.Append(c);
        }
        return sb.ToString();
    }

    #endregion

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
