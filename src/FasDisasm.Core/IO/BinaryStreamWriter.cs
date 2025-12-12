using System.Text;

namespace FasDisasm.Core.IO;

/// <summary>
/// A binary stream writer for creating FAS file formats.
/// </summary>
public class BinaryStreamWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly BinaryWriter _writer;
    private readonly bool _ownsStream;
    private bool _disposed;

    /// <summary>
    /// Gets the file name if writing to a file.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Gets or sets the current position in the stream.
    /// </summary>
    public long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <summary>
    /// Gets the total length of the stream.
    /// </summary>
    public long Length => _stream.Length;

    /// <summary>
    /// Creates a new BinaryStreamWriter to a file.
    /// </summary>
    public BinaryStreamWriter(string fileName, bool overwrite = true)
    {
        FileName = fileName;
        var mode = overwrite ? FileMode.Create : FileMode.OpenOrCreate;
        _stream = new FileStream(fileName, mode, FileAccess.Write, FileShare.None);
        _writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: false);
        _ownsStream = true;
    }

    /// <summary>
    /// Creates a new BinaryStreamWriter to a memory stream.
    /// </summary>
    public BinaryStreamWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: true);
        _ownsStream = true;
    }

    /// <summary>
    /// Creates a new BinaryStreamWriter to an existing stream.
    /// </summary>
    public BinaryStreamWriter(Stream stream, bool ownsStream = false)
    {
        _stream = stream;
        _writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: !ownsStream);
        _ownsStream = ownsStream;
    }

    /// <summary>
    /// Writes a single byte.
    /// </summary>
    public void WriteByte(byte value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed byte.
    /// </summary>
    public void WriteSByte(sbyte value) => _writer.Write(value);

    /// <summary>
    /// Writes an unsigned 16-bit integer.
    /// </summary>
    public void WriteUInt16(ushort value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed 16-bit integer.
    /// </summary>
    public void WriteInt16(short value) => _writer.Write(value);

    /// <summary>
    /// Writes an unsigned 32-bit integer.
    /// </summary>
    public void WriteUInt32(uint value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed 32-bit integer.
    /// </summary>
    public void WriteInt32(int value) => _writer.Write(value);

    /// <summary>
    /// Writes an unsigned 64-bit integer.
    /// </summary>
    public void WriteUInt64(ulong value) => _writer.Write(value);

    /// <summary>
    /// Writes a signed 64-bit integer.
    /// </summary>
    public void WriteInt64(long value) => _writer.Write(value);

    /// <summary>
    /// Writes a double-precision floating-point number.
    /// </summary>
    public void WriteDouble(double value) => _writer.Write(value);

    /// <summary>
    /// Writes a single-precision floating-point number.
    /// </summary>
    public void WriteSingle(float value) => _writer.Write(value);

    /// <summary>
    /// Writes a byte array.
    /// </summary>
    public void WriteBytes(byte[] data) => _writer.Write(data);

    /// <summary>
    /// Writes a fixed-length string (padded with nulls if needed).
    /// </summary>
    public void WriteFixedString(string value, int length)
    {
        var bytes = new byte[length];
        var strBytes = Encoding.ASCII.GetBytes(value);
        Array.Copy(strBytes, bytes, Math.Min(strBytes.Length, length));
        _writer.Write(bytes);
    }

    /// <summary>
    /// Writes a null-terminated string.
    /// </summary>
    public void WriteNullTerminatedString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        _writer.Write(bytes);
        _writer.Write((byte)0);
    }

    /// <summary>
    /// Writes a string without null terminator.
    /// </summary>
    public void WriteString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        _writer.Write(bytes);
    }

    /// <summary>
    /// Flushes the writer.
    /// </summary>
    public void Flush() => _writer.Flush();

    /// <summary>
    /// Gets the underlying stream as a byte array (only for memory streams).
    /// </summary>
    public byte[] ToArray()
    {
        if (_stream is MemoryStream ms)
        {
            return ms.ToArray();
        }
        throw new InvalidOperationException("ToArray is only supported for memory streams.");
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
                _writer.Flush();
                _writer.Dispose();
                if (_ownsStream)
                {
                    _stream.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
