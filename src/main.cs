using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

const string Marker      = "BINARY_DUMP_V2";
const string HashLabel   = "SHA256:";
const string SizeLabel   = "SIZE:";
const string ExtLabel    = "EXT:";
const string CompLabel   = "COMPRESSED:";
const int    LineWidth   = 76;

static string Hash(byte[] data)
{
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(data));
}

static byte[] Compress(byte[] data)
{
    using var ms = new MemoryStream();
    using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize))
        gz.Write(data);
    return ms.ToArray();
}

static byte[] Decompress(byte[] data)
{
    using var input  = new MemoryStream(data);
    using var output = new MemoryStream();
    using (var gz = new GZipStream(input, CompressionMode.Decompress))
        gz.CopyTo(output);
    return output.ToArray();
}

static void Deconstruct(string src, string dst)
{
    byte[] original   = File.ReadAllBytes(src);
    string hash       = Hash(original);
    string ext        = Path.GetExtension(src);
    byte[] compressed = Compress(original);

    bool didCompress  = compressed.Length < original.Length;
    byte[] payload    = didCompress ? compressed : original;
    string b64        = Convert.ToBase64String(payload);

    Console.WriteLine($"Read       : {src} ({original.Length} bytes)");
    Console.WriteLine($"SHA256     : {hash}");
    Console.WriteLine($"Compressed : {compressed.Length} bytes " +
                      $"({100.0 * compressed.Length / original.Length:F1}%)");
    if (!didCompress)
        Console.WriteLine("Note       : compression made it larger, storing raw");
    Console.WriteLine($"Base64     : {b64.Length} chars");

    using var w = new StreamWriter(dst, append: false, Encoding.ASCII);
    w.WriteLine(Marker);
    w.WriteLine($"{HashLabel}{hash}");
    w.WriteLine($"{SizeLabel}{original.Length}");
    w.WriteLine($"{ExtLabel}{ext}");
    w.WriteLine($"{CompLabel}{didCompress}");

    for (int i = 0; i < b64.Length; i += LineWidth)
        w.WriteLine(b64.AsSpan(i, Math.Min(LineWidth, b64.Length - i)));

    Console.WriteLine($"Written    : {dst}");
}

static void Reconstruct(string src, string dst)
{
    using var r = new StreamReader(src, Encoding.ASCII);

    string? marker = r.ReadLine();
    if (marker != Marker)
        throw new InvalidDataException($"Bad marker: '{marker}'");

    string hashLine = r.ReadLine() ?? throw new InvalidDataException("Missing hash line");
    if (!hashLine.StartsWith(HashLabel)) throw new InvalidDataException("Malformed hash line");
    string expectedHash = hashLine[HashLabel.Length..];

    string sizeLine = r.ReadLine() ?? throw new InvalidDataException("Missing size line");
    if (!sizeLine.StartsWith(SizeLabel)) throw new InvalidDataException("Malformed size line");
    int expectedSize = int.Parse(sizeLine[SizeLabel.Length..]);

    string extLine = r.ReadLine() ?? throw new InvalidDataException("Missing ext line");
    string originalExt = extLine.StartsWith(ExtLabel) ? extLine[ExtLabel.Length..] : "";

    string compLine = r.ReadLine() ?? throw new InvalidDataException("Missing compressed line");
    if (!compLine.StartsWith(CompLabel)) throw new InvalidDataException("Malformed compressed line");
    bool wasCompressed = bool.Parse(compLine[CompLabel.Length..]);

    if (!string.IsNullOrEmpty(originalExt) && Path.GetExtension(dst) != originalExt)
        Console.WriteLine($"Warning: original extension was '{originalExt}', output is '{Path.GetExtension(dst)}'");

    var sb = new StringBuilder();
    string? line;
    while ((line = r.ReadLine()) != null)
        if (!string.IsNullOrWhiteSpace(line))
            sb.Append(line);

    byte[] payload = Convert.FromBase64String(sb.ToString());
    byte[] data    = wasCompressed ? Decompress(payload) : payload;

    if (data.Length != expectedSize)
        throw new InvalidDataException($"Size mismatch: expected {expectedSize}, got {data.Length}");

    string actualHash = Hash(data);
    if (actualHash != expectedHash)
        throw new InvalidDataException(
            $"Hash mismatch\n  expected : {expectedHash}\n  actual   : {actualHash}");

    File.WriteAllBytes(dst, data);
    Console.WriteLine($"SHA256 verified : {actualHash}");
    Console.WriteLine($"Reconstructed   : {dst} ({data.Length} bytes)");
}

// ── arg parsing ───────────────────────────────────────────────────────────────

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  bindump -d <input>     <output.txt>");
    Console.Error.WriteLine("  bindump -r <input.txt> <output>");
}

if (args.Length != 3 || (args[0] != "-d" && args[0] != "-r"))
{
    PrintUsage();
    Environment.Exit(1);
}

try
{
    if (args[0] == "-d") Deconstruct(args[1], args[2]);
    else                 Reconstruct(args[1], args[2]);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(2);
}