using FasDisasm.Core.Disassembly;

namespace FasDisasm.Core.IO;

/// <summary>
/// Extracts FAS files from VLX (Visual Lisp eXecutable) archives.
/// VLX files are archives that contain multiple FAS files.
/// </summary>
public class VlxSplitter
{
    private const string VlxSignature = "VRTLIB";
    private const string FasSignature = "FAS";

    /// <summary>
    /// Gets the list of extracted FAS file entries.
    /// </summary>
    public List<VlxEntry> Entries { get; } = new();

    /// <summary>
    /// Event raised during extraction progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? Progress;

    /// <summary>
    /// Checks if the file is a VLX archive.
    /// </summary>
    public static bool IsVlxFile(string fileName)
    {
        try
        {
            using var stream = File.OpenRead(fileName);
            var header = new byte[6];
            if (stream.Read(header, 0, 6) < 6)
                return false;

            return System.Text.Encoding.ASCII.GetString(header) == VlxSignature;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads and parses a VLX file.
    /// </summary>
    public void Load(string fileName)
    {
        Entries.Clear();

        using var reader = new BinaryStreamReader(fileName);

        // Verify signature
        var signature = reader.ReadFixedString(6);
        if (signature != VlxSignature)
        {
            throw new InvalidDataException($"Invalid VLX file: expected '{VlxSignature}' signature.");
        }

        // Read VLX header
        // The header contains information about embedded files

        // Skip header padding
        reader.Skip(2); // Usually 0x00 0x00

        // Read version or flags
        var version = reader.ReadUInt32();

        // Read number of entries or file table offset
        var numEntries = reader.ReadUInt32();

        // Parse the file table
        for (uint i = 0; i < numEntries; i++)
        {
            var entry = ParseEntry(reader);
            if (entry != null)
            {
                Entries.Add(entry);
            }

            Progress?.Invoke(this, new ProgressEventArgs(i + 1, numEntries));
        }
    }

    private VlxEntry? ParseEntry(BinaryStreamReader reader)
    {
        try
        {
            // Read entry header
            var nameLength = reader.ReadByte();
            if (nameLength == 0)
                return null;

            var name = reader.ReadFixedString(nameLength);
            var dataOffset = reader.ReadUInt32();
            var dataLength = reader.ReadUInt32();

            return new VlxEntry
            {
                Name = name,
                Offset = dataOffset,
                Length = dataLength
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts all FAS files from the VLX to the specified directory.
    /// </summary>
    public List<string> ExtractAll(string vlxFileName, string outputDirectory)
    {
        var extractedFiles = new List<string>();

        if (Entries.Count == 0)
        {
            Load(vlxFileName);
        }

        Directory.CreateDirectory(outputDirectory);

        using var reader = new BinaryStreamReader(vlxFileName);

        foreach (var entry in Entries)
        {
            var outputPath = Path.Combine(outputDirectory, entry.Name);

            reader.Position = entry.Offset;
            var data = reader.ReadBytes((int)entry.Length);

            File.WriteAllBytes(outputPath, data);
            extractedFiles.Add(outputPath);
        }

        return extractedFiles;
    }

    /// <summary>
    /// Extracts a single entry by name.
    /// </summary>
    public byte[] ExtractEntry(string vlxFileName, string entryName)
    {
        if (Entries.Count == 0)
        {
            Load(vlxFileName);
        }

        var entry = Entries.FirstOrDefault(e =>
            e.Name.Equals(entryName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new FileNotFoundException($"Entry '{entryName}' not found in VLX file.");
        }

        using var reader = new BinaryStreamReader(vlxFileName);
        reader.Position = entry.Offset;
        return reader.ReadBytes((int)entry.Length);
    }
}

/// <summary>
/// Represents an entry in a VLX archive.
/// </summary>
public class VlxEntry
{
    /// <summary>
    /// Gets or sets the entry name (file name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the offset of the data in the VLX file.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the length of the data.
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    /// Gets the file extension.
    /// </summary>
    public string Extension => Path.GetExtension(Name).ToLowerInvariant();

    /// <summary>
    /// Gets whether this entry is a FAS file.
    /// </summary>
    public bool IsFasFile => Extension is ".fas" or ".fsl";

    public override string ToString() => $"{Name} ({Length} bytes @ 0x{Offset:X})";
}
