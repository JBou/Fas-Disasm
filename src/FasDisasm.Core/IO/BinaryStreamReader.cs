using System.Text;

namespace FasDisasm.Core.IO;

/// <summary>
/// A binary stream reader optimized for reading FAS file formats.
/// Supports both file-based and memory-based (byte array) reading.
/// </summary>
public class BinaryStreamReader : IDisposable
{
    private readonly Stream _stream;
    private readonly BinaryReader _reader;
    private readonly bool _ownsStream;
    private bool _disposed;

    /// <summary>
    /// Gets the file name if reading from a file.
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
    /// Gets whether the stream is at the end.
    /// </summary>
    public bool EndOfStream => Position >= Length;

    /// <summary>
    /// Gets the number of bytes remaining in the stream.
    /// </summary>
    public long Remaining => Length - Position;

    /// <summary>
    /// Creates a new BinaryStreamReader from a file.
    /// </summary>
    public BinaryStreamReader(string fileName)
    {
        FileName = fileName;
        _stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: false);
        _ownsStream = true;
    }

    /// <summary>
    /// Creates a new BinaryStreamReader from a byte array.
    /// </summary>
    public BinaryStreamReader(byte[] data)
    {
        _stream = new MemoryStream(data, writable: false);
        _reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: false);
        _ownsStream = true;
    }

    /// <summary>
    /// Creates a new BinaryStreamReader from an existing stream.
    /// </summary>
    public BinaryStreamReader(Stream stream, bool ownsStream = false)
    {
        _stream = stream;
        _reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: !ownsStream);
        _ownsStream = ownsStream;
    }

    /// <summary>
    /// Reads a single byte (unsigned).
    /// </summary>
    public byte ReadByte() => _reader.ReadByte();

    /// <summary>
    /// Reads a single signed byte.
    /// </summary>
    public sbyte ReadSByte() => _reader.ReadSByte();

    /// <summary>
    /// Reads an unsigned 8-bit integer.
    /// </summary>
    public byte ReadUInt8() => _reader.ReadByte();

    /// <summary>
    /// Reads a signed 8-bit integer.
    /// </summary>
    public sbyte ReadInt8() => _reader.ReadSByte();

    /// <summary>
    /// Reads an unsigned 16-bit integer (little-endian).
    /// </summary>
    public ushort ReadUInt16() => _reader.ReadUInt16();

    /// <summary>
    /// Reads a signed 16-bit integer (little-endian).
    /// </summary>
    public short ReadInt16() => _reader.ReadInt16();

    /// <summary>
    /// Reads an unsigned 32-bit integer (little-endian).
    /// </summary>
    public uint ReadUInt32() => _reader.ReadUInt32();

    /// <summary>
    /// Reads a signed 32-bit integer (little-endian).
    /// </summary>
    public int ReadInt32() => _reader.ReadInt32();

    /// <summary>
    /// Reads an unsigned 64-bit integer (little-endian).
    /// </summary>
    public ulong ReadUInt64() => _reader.ReadUInt64();

    /// <summary>
    /// Reads a signed 64-bit integer (little-endian).
    /// </summary>
    public long ReadInt64() => _reader.ReadInt64();

    /// <summary>
    /// Reads a double-precision floating-point number.
    /// </summary>
    public double ReadDouble() => _reader.ReadDouble();

    /// <summary>
    /// Reads a single-precision floating-point number.
    /// </summary>
    public float ReadSingle() => _reader.ReadSingle();

    /// <summary>
    /// Reads a specified number of bytes.
    /// </summary>
    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

    /// <summary>
    /// Reads a fixed-length string (ASCII encoding).
    /// </summary>
    public string ReadFixedString(int length)
    {
        var bytes = _reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Reads a null-terminated string (ASCII encoding).
    /// </summary>
    public string ReadNullTerminatedString()
    {
        var sb = new StringBuilder();
        byte b;
        while ((b = _reader.ReadByte()) != 0)
        {
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Reads a string terminated by a specific character.
    /// </summary>
    public string ReadTerminatedString(char terminator)
    {
        var sb = new StringBuilder();
        byte b;
        while ((b = _reader.ReadByte()) != terminator)
        {
            sb.Append((char)b);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Reads all remaining bytes from the current position.
    /// </summary>
    public byte[] ReadToEnd()
    {
        var remaining = (int)Remaining;
        return _reader.ReadBytes(remaining);
    }

    /// <summary>
    /// Reads all remaining bytes as a string.
    /// </summary>
    public string ReadToEndAsString()
    {
        var bytes = ReadToEnd();
        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Moves the position forward by the specified number of bytes.
    /// </summary>
    public void Skip(int count)
    {
        Position += count;
    }

    /// <summary>
    /// Moves the position backward by the specified number of bytes.
    /// </summary>
    public void Rewind(int count)
    {
        Position -= count;
    }

    /// <summary>
    /// Seeks to a specific position from the beginning.
    /// </summary>
    public void SeekBegin(long offset)
    {
        _stream.Seek(offset, SeekOrigin.Begin);
    }

    /// <summary>
    /// Seeks to a specific position from the end.
    /// </summary>
    public void SeekEnd(long offset)
    {
        _stream.Seek(offset, SeekOrigin.End);
    }

    /// <summary>
    /// Peeks at the next byte without advancing the position.
    /// </summary>
    public byte PeekByte()
    {
        var b = _reader.ReadByte();
        Position--;
        return b;
    }

    /// <summary>
    /// Peeks at the next N bytes without advancing the position.
    /// </summary>
    public byte[] PeekBytes(int count)
    {
        var bytes = _reader.ReadBytes(count);
        Position -= bytes.Length;
        return bytes;
    }

    /// <summary>
    /// Searches for a byte sequence starting from the current position.
    /// Returns the offset if found, or -1 if not found.
    /// </summary>
    public long FindBytes(byte[] pattern)
    {
        var startPos = Position;
        var buffer = new byte[pattern.Length];

        while (Remaining >= pattern.Length)
        {
            var currentPos = Position;
            var bytesRead = _stream.Read(buffer, 0, pattern.Length);

            if (bytesRead < pattern.Length)
                break;

            if (buffer.SequenceEqual(pattern))
            {
                Position = currentPos;
                return currentPos;
            }

            Position = currentPos + 1;
        }

        Position = startPos;
        return -1;
    }

    /// <summary>
    /// Searches for a string starting from the current position.
    /// Returns the offset if found, or -1 if not found.
    /// </summary>
    public long FindString(string pattern)
    {
        return FindBytes(Encoding.ASCII.GetBytes(pattern));
    }

    /// <summary>
    /// Creates a sub-stream from the current position with the specified length.
    /// </summary>
    public BinaryStreamReader CreateSubStream(int length)
    {
        var data = ReadBytes(length);
        return new BinaryStreamReader(data);
    }

    /// <summary>
    /// Gets all data from the stream as a byte array.
    /// </summary>
    public byte[] ToArray()
    {
        var currentPos = Position;
        Position = 0;
        var data = ReadBytes((int)Length);
        Position = currentPos;
        return data;
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
                _reader.Dispose();
                if (_ownsStream)
                {
                    _stream.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
